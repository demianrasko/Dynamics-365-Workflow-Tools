<#
.SYNOPSIS
    Builds the upgrade test workflows (testing\Upgrade test workflows.md) from WFT 03b to WFT 16, and WFT Test Lead.

.DESCRIPTION
    Generates each workflow's steps, with every input filled in, and saves it in the Dynamics 365 test environment
    (DATAVERSE_CONNECTION, saved by Set-DataverseTestConnections.ps1) as a draft.

    - The steps are made by UpgradeTestWorkflows\upgrade_workflows.py (Python 3), the same way the workflow designer
      writes them. Each step lists the inputs and outputs of the installed activity, read from its registration.
    - The test records are looked up by name, so run New-UpgradeTestData.ps1 first.
    - A workflow that doesn't exist yet is created in the UpgradeTestWorkflows solution. An existing workflow is only
      filled when it is an empty draft; one that already has steps is left alone, unless it's named in -Replace.
    - New workflows aren't activated: open each one in the designer, check it and activate it. A replaced workflow that
      was activated is deactivated, updated and activated again.

    "A test user" in the test plan is the application user the script connects as, so no real person is shared with
    or loses a role.

.PARAMETER ProcessStage
    The WFT Test BPF stage that Set Process Stage moves to. Default "Stage 2".

.PARAMETER Solution
    The unmanaged solution new workflows go into. Default UpgradeTestWorkflows.

.PARAMETER OutputFolder
    Where the generated XAML is written. Default a folder in %TEMP%.

.PARAMETER Replace
    Workflows to build again even though they have steps, by number (e.g. 05, 11a) or name.

.PARAMETER GenerateOnly
    Only writes the XAML to OutputFolder; saves nothing in the environment.

.PARAMETER ConnectionVariable
    The environment variable with the connection string. Default DATAVERSE_CONNECTION (the Dynamics 365 test environment).

.EXAMPLE
    .\tools\Publish-UpgradeTestWorkflows.ps1

.EXAMPLE
    .\tools\Publish-UpgradeTestWorkflows.ps1 -Replace 05, 06

.EXAMPLE
    .\tools\Publish-UpgradeTestWorkflows.ps1 -GenerateOnly -OutputFolder C:\Temp\wft
#>
[CmdletBinding()]
param(
    [string]$ProcessStage = 'Stage 2',
    [string]$Solution = 'UpgradeTestWorkflows',
    [string]$OutputFolder = (Join-Path ([IO.Path]::GetTempPath()) 'UpgradeTestWorkflows'),
    [string[]]$Replace,
    [switch]$GenerateOnly,
    [string]$ConnectionVariable = 'DATAVERSE_CONNECTION'
)

$ErrorActionPreference = 'Stop'

# pwsh -File passes "05,06" as one value
$Replace = @($Replace -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })

# connection and Web API helpers
. (Join-Path $PSScriptRoot 'Dataverse.ps1')

# Finds a test record by name; returns what a lookup step needs.
function Get-TestRecord([string]$entity, [string]$entitySet, [string]$idColumn, [string]$nameColumn, [string]$filter) {
    $record = Get-Records $entitySet "`$select=$idColumn,$nameColumn&`$filter=$filter&`$orderby=createdon desc&`$top=1"

    if ($record.Count -eq 0) {
        throw "No $entity matches $filter. Run tools\New-UpgradeTestData.ps1 first."
    }

    $id = $record[0].$idColumn

    return @{ entity = $entity; id = $id; name = $record[0].$nameColumn; url = Get-RecordUrl $entity $id }
}

# ---- the installed workflow tools ----------------------------------------------------------------------------------

$assembly = Get-Records 'pluginassemblies' "`$select=pluginassemblyid,version&`$filter=name eq 'msdyncrmWorkflowTools'"

if ($assembly.Count -eq 0) {
    throw 'The workflow tools (msdyncrmWorkflowTools) are not installed in this environment.'
}

Write-Host "Workflow tools: $($assembly[0].version)"

$activities = @(Get-Records 'plugintypes' "`$select=typename,customworkflowactivityinfo&`$filter=_pluginassemblyid_value eq $($assembly[0].pluginassemblyid) and isworkflowactivity eq true" |
    ForEach-Object { @{ typename = $_.typename; info = $_.customworkflowactivityinfo } })

# ---- the test records ----------------------------------------------------------------------------------------------

$rootBusinessUnit = Get-TestRecord 'businessunit' 'businessunits' 'businessunitid' 'name' '_parentbusinessunitid_value eq null'
$me = Invoke-Api GET 'WhoAmI'

$product = Get-TestRecord 'product' 'products' 'productid' 'name' "productnumber eq 'WFT-PRODUCT'"
$productUnit = (Invoke-Api GET "products($($product.id))?`$select=_defaultuomid_value")._defaultuomid_value

$records = @{
    team             = Get-TestRecord 'team' 'teams' 'teamid' 'name' "name eq 'WFT Team'"
    role             = Get-TestRecord 'role' 'roles' 'roleid' 'name' "name eq 'WFT Role' and _businessunitid_value eq $($rootBusinessUnit.id)"
    basicUser        = Get-TestRecord 'role' 'roles' 'roleid' 'name' "name eq 'Basic User' and _businessunitid_value eq $($rootBusinessUnit.id)"
    queue            = Get-TestRecord 'queue' 'queues' 'queueid' 'name' "name eq 'WFT Queue'"
    list             = Get-TestRecord 'list' 'lists' 'listid' 'listname' "listname eq 'WFT List'"
    list2            = Get-TestRecord 'list' 'lists' 'listid' 'listname' "listname eq 'WFT List 2'"
    dynamicList      = Get-TestRecord 'list' 'lists' 'listid' 'listname' "listname eq 'WFT Dynamic List'"
    campaign         = Get-TestRecord 'campaign' 'campaigns' 'campaignid' 'name' "name eq 'WFT Campaign'"
    goal             = Get-TestRecord 'goal' 'goals' 'goalid' 'title' "title eq 'WFT Goal'"
    literature       = Get-TestRecord 'salesliterature' 'salesliteratures' 'salesliteratureid' 'name' "name eq 'WFT Literature'"
    template         = Get-TestRecord 'template' 'templates' 'templateid' 'title' "title eq 'WFT Template'"
    bpf              = Get-TestRecord 'workflow' 'workflows' 'workflowid' 'name' "name eq 'WFT Test BPF' and type eq 1 and category eq 4"
    wftTest          = Get-TestRecord 'workflow' 'workflows' 'workflowid' 'name' "name eq 'WFT Test' and type eq 1 and category eq 0"
    rootBusinessUnit = $rootBusinessUnit
    contact1         = Get-TestRecord 'contact' 'contacts' 'contactid' 'fullname' "fullname eq 'WFT Contact 1'"
    testUser         = Get-TestRecord 'systemuser' 'systemusers' 'systemuserid' 'fullname' "systemuserid eq $($me.UserId)"
    account          = Get-TestRecord 'account' 'accounts' 'accountid' 'name' "name eq 'WFT Upgrade Account'"
    # the newest, even when 04 has deactivated it: 05 only maps values onto it
    scratch          = Get-TestRecord 'account' 'accounts' 'accountid' 'name' "name eq 'WFT Scratch Account'"
    lead             = Get-TestRecord 'lead' 'leads' 'leadid' 'fullname' "emailaddress1 eq 'wft-lead@example.com'"
    opportunity      = Get-TestRecord 'opportunity' 'opportunities' 'opportunityid' 'name' "name eq 'WFT Opportunity'"
    product          = $product
    uom              = Get-TestRecord 'uom' 'uoms' 'uomid' 'name' "uomid eq $productUnit"
    choiceTwo        = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'Two'
    processStage     = $ProcessStage
    # Join builds the clone's URL from this and the cloned id
    accountUrlStart  = (Get-RecordUrl 'account' '') -replace '&pagetype=entityrecord$', ''
}

$workflows = @{}
$existing = @{}

foreach ($workflow in Get-Records 'workflows' "`$select=workflowid,name,statecode,xaml&`$filter=startswith(name,'WFT ') and type eq 1 and category eq 0") {
    $workflows[$workflow.name] = $workflow.workflowid
    $existing[$workflow.name] = $workflow
}

# ---- generate ------------------------------------------------------------------------------------------------------

$python = Get-Command python, py -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $python) {
    throw 'The generator needs Python 3 (python or py on the PATH).'
}

New-Item -ItemType Directory -Force $OutputFolder | Out-Null
$inputFile = Join-Path $OutputFolder 'input.json'
$data = @{ org = $url; activities = $activities; records = $records; workflows = $workflows } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($inputFile, $data, [Text.UTF8Encoding]::new($false))

& $python.Source (Join-Path $PSScriptRoot 'UpgradeTestWorkflows\upgrade_workflows.py') $inputFile $OutputFolder

if ($LASTEXITCODE -ne 0) {
    throw 'Generating the workflows failed.'
}

$manifest = Get-Content (Join-Path $OutputFolder 'manifest.json') -Raw | ConvertFrom-Json
Write-Host "Generated $($manifest.Count) workflows in $OutputFolder"

if ($GenerateOnly) {
    return
}

# ---- save ----------------------------------------------------------------------------------------------------------

foreach ($item in $manifest) {
    $xaml = [IO.File]::ReadAllText($item.file)

    if ($item.new) {
        $headers['MSCRM.SolutionUniqueName'] = $Solution

        try {
            Invoke-Api POST 'workflows' @{
                workflowid      = $item.id
                name            = $item.name
                type            = 1
                category        = 0
                primaryentity   = $item.primary
                scope           = 4
                ondemand        = $true
                triggeroncreate = $false
                subprocess      = $false
                runas           = 1
                mode            = 0
                asyncautodelete = $true
                xaml            = $xaml
            } | Out-Null
        }
        finally {
            $headers.Remove('MSCRM.SolutionUniqueName')
        }

        $action = "created in $Solution (draft)"
    }
    else {
        $current = $existing[$item.name]
        $name = $item.name
        $replacing = [bool]($Replace | Where-Object { $name -eq $_ -or $name -like "WFT $_ *" })

        if (-not $replacing -and ($current.statecode -ne 0 -or $current.xaml -notmatch '<mxswa:Workflow\s*/>')) {
            Write-Host "$($item.name): has steps or is active, left alone"
            continue
        }

        # an activated workflow can't be changed
        if ($current.statecode -eq 1) {
            Invoke-Api PATCH "workflows($($item.id))" @{ statecode = 0; statuscode = 1 } | Out-Null
        }

        Invoke-Api PATCH "workflows($($item.id))" @{ xaml = $xaml } | Out-Null
        $action = if ($replacing) { 'replaced' } else { 'filled in' }

        if ($current.statecode -eq 1) {
            Invoke-Api PATCH "workflows($($item.id))" @{ statecode = 1; statuscode = 2 } | Out-Null
            $action += ' and activated again'
        }
    }

    $saved = Invoke-Api GET "workflows($($item.id))?`$select=xaml"

    if ($saved.xaml -ne $xaml) {
        throw "$($item.name): what was saved differs from what was sent."
    }

    Write-Host "$($item.name): $action" -ForegroundColor Green
}

Write-Host ''
Write-Host 'Open each new or filled-in workflow in the designer, check its steps and activate it.' -ForegroundColor Cyan
