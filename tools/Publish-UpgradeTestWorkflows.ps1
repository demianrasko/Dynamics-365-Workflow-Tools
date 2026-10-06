<#
.SYNOPSIS
    Builds the upgrade test workflows (testing\Upgrade test workflows.md) from WFT 03b to WFT 13.

.DESCRIPTION
    Generates each workflow's steps, with every input filled in, and saves it in the Dynamics 365 test environment
    (DATAVERSE_CONNECTION, saved by Set-DataverseTestConnections.ps1) as a draft.

    - The steps are made by UpgradeTestWorkflows\upgrade_workflows.py (Python 3), the same way the workflow designer
      writes them. Each step lists the inputs and outputs of the installed activity, read from its registration.
    - The test records are looked up by name, so run New-UpgradeTestData.ps1 first.
    - A workflow that doesn't exist yet is created in the UpgradeTestWorkflows solution. An existing workflow is only
      filled when it is an empty draft; one that already has steps is left alone.
    - Nothing is activated: open each workflow in the designer, check it and activate it.

    "A test user" in the test plan is the application user the script connects as, so no real person is shared with
    or loses a role.

.PARAMETER ProcessStage
    The WFT Test BPF stage that Set Process Stage moves to. Default "Stage 2".

.PARAMETER Solution
    The unmanaged solution new workflows go into. Default UpgradeTestWorkflows.

.PARAMETER OutputFolder
    Where the generated XAML is written. Default a folder in %TEMP%.

.PARAMETER GenerateOnly
    Only writes the XAML to OutputFolder; saves nothing in the environment.

.PARAMETER ConnectionVariable
    The environment variable with the connection string. Default DATAVERSE_CONNECTION (the Dynamics 365 test environment).

.EXAMPLE
    .\tools\Publish-UpgradeTestWorkflows.ps1

.EXAMPLE
    .\tools\Publish-UpgradeTestWorkflows.ps1 -GenerateOnly -OutputFolder C:\Temp\wft
#>
[CmdletBinding()]
param(
    [string]$ProcessStage = 'Stage 2',
    [string]$Solution = 'UpgradeTestWorkflows',
    [string]$OutputFolder = (Join-Path ([IO.Path]::GetTempPath()) 'UpgradeTestWorkflows'),
    [switch]$GenerateOnly,
    [string]$ConnectionVariable = 'DATAVERSE_CONNECTION'
)

$ErrorActionPreference = 'Stop'

# ---- connection ----------------------------------------------------------------------------------------------------

function ConvertFrom-ConnectionString([string]$connectionString) {
    $values = @{}

    foreach ($part in $connectionString -split ';') {
        $pair = $part -split '=', 2

        if ($pair.Count -eq 2) {
            $values[$pair[0].Trim().ToLowerInvariant()] = $pair[1].Trim()
        }
    }

    return $values
}

$connectionString = [Environment]::GetEnvironmentVariable($ConnectionVariable)

if (-not $connectionString) {
    $connectionString = [Environment]::GetEnvironmentVariable($ConnectionVariable, 'User')
}

if (-not $connectionString) {
    throw "Set $ConnectionVariable first (tools\Set-DataverseTestConnections.ps1)."
}

$settings = ConvertFrom-ConnectionString $connectionString
$url = $settings['url'].TrimEnd('/')
$api = "$url/api/data/v9.2"
$tenant = $settings['tenantid']

if (-not $tenant) {
    try {
        Invoke-WebRequest -Uri "$api/" -UseBasicParsing | Out-Null
    }
    catch {
        $challenge = $_.Exception.Response.Headers['WWW-Authenticate']

        if (-not $challenge) {
            $challenge = ($_.Exception.Response.Headers | Where-Object { $_.Key -eq 'WWW-Authenticate' }).Value -join ' '
        }

        if ($challenge -match 'login\.microsoftonline\.com/([0-9a-fA-F-]{36})') {
            $tenant = $Matches[1]
        }
    }
}

$token = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$tenant/oauth2/v2.0/token" -Body @{
    grant_type    = 'client_credentials'
    client_id     = $settings['clientid']
    client_secret = $settings['clientsecret']
    scope         = "$url/.default"
}

$headers = @{
    Authorization      = "Bearer $($token.access_token)"
    Accept             = 'application/json'
    'OData-Version'    = '4.0'
    'OData-MaxVersion' = '4.0'
    Prefer             = 'odata.include-annotations="*"'
}

Write-Host "Environment: $url"

# ---- Web API helpers -----------------------------------------------------------------------------------------------

function Get-Error($errorRecord) {
    try {
        return ($errorRecord.ErrorDetails.Message | ConvertFrom-Json).error.message
    }
    catch {
        return $errorRecord.Exception.Message
    }
}

function Invoke-Api([string]$method, [string]$path, $body) {
    $arguments = @{ Method = $method; Uri = "$api/$path"; Headers = $headers; UseBasicParsing = $true }

    if ($null -ne $body) {
        $arguments.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10 -Compress))
        $arguments.ContentType = 'application/json; charset=utf-8'
    }

    try {
        $response = Invoke-WebRequest @arguments
    }
    catch {
        throw "$method $path failed: $(Get-Error $_)"
    }

    $location = @($response.Headers['OData-EntityId'])[0]

    if ($location -match '\(([0-9a-fA-F-]{36})\)$') {
        return $Matches[1]
    }

    if ($response.Content) {
        return $response.Content | ConvertFrom-Json
    }
}

function Get-Records([string]$entitySet, [string]$query) {
    return @((Invoke-Api GET "$entitySet`?$query").value)
}

function Escape-OData([string]$text) {
    return $text.Replace("'", "''")
}


# Finds a test record by name; returns what a lookup step needs.
function Get-TestRecord([string]$entity, [string]$entitySet, [string]$idColumn, [string]$nameColumn, [string]$filter) {
    $record = Get-Records $entitySet "`$select=$idColumn,$nameColumn&`$filter=$filter&`$orderby=createdon desc&`$top=1"

    if ($record.Count -eq 0) {
        throw "No $entity matches $filter. Run tools\New-UpgradeTestData.ps1 first."
    }

    return @{ entity = $entity; id = $record[0].$idColumn; name = $record[0].$nameColumn }
}

function Get-OptionValue([string]$entity, [string]$column, [string]$type, [string]$label) {
    $metadata = Invoke-Api GET "EntityDefinitions(LogicalName='$entity')/Attributes(LogicalName='$column')/Microsoft.Dynamics.CRM.$type`?`$select=LogicalName&`$expand=OptionSet(`$select=Options)"
    $option = $metadata.OptionSet.Options | Where-Object { $_.Label.UserLocalizedLabel.Label -eq $label } | Select-Object -First 1

    if (-not $option) {
        throw "$entity.$column has no option '$label'. Run the integration tests once to create the test columns."
    }

    return $option.Value
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
    scratch          = Get-TestRecord 'account' 'accounts' 'accountid' 'name' "name eq 'WFT Scratch Account' and statecode eq 0"
    lead             = Get-TestRecord 'lead' 'leads' 'leadid' 'fullname' "emailaddress1 eq 'wft-lead@example.com'"
    opportunity      = Get-TestRecord 'opportunity' 'opportunities' 'opportunityid' 'name' "name eq 'WFT Opportunity'"
    choiceTwo        = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'Two'
    processStage     = $ProcessStage
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

        $action = "created in $Solution"
    }
    else {
        $current = $existing[$item.name]

        if ($current.statecode -ne 0 -or $current.xaml -notmatch '<mxswa:Workflow\s*/>') {
            Write-Host "$($item.name): has steps or is active, left alone"
            continue
        }

        Invoke-Api PATCH "workflows($($item.id))" @{ xaml = $xaml } | Out-Null
        $action = 'filled in'
    }

    $saved = Invoke-Api GET "workflows($($item.id))?`$select=xaml"

    if ($saved.xaml -ne $xaml) {
        throw "$($item.name): what was saved differs from what was sent."
    }

    Write-Host "$($item.name): $action (draft)" -ForegroundColor Green
}

Write-Host ''
Write-Host 'Open each workflow in the designer, check its steps and activate it.' -ForegroundColor Cyan
