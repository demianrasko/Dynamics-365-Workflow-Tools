<#
.SYNOPSIS
    Runs every upgrade test workflow (testing\Upgrade test workflows.md) and saves what each one did.

.DESCRIPTION
    For each WFT workflow, in the order of the test plan:

    1. Starts it on its test record and waits for the system job to finish.
    2. Collects the log notes it wrote on the record, and checks the records it created or changed (for example the
       qualified lead's account, contact and opportunity, or the emails 09 created and their attachments).

    Before the run it refreshes the test data (New-UpgradeTestData.ps1) and puts back what earlier runs changed, so
    every run starts the same way. After the run it deletes what the workflows leave behind: the cloned contacts
    (03b), WFT Created Team (07), the static lists Copy To Static List makes (11a) and the account, contact and
    opportunity qualifying the lead creates (11b; duplicate detection would stop the next run).

    The results go to testing\results\<date> <label>\ (not committed): results.json with everything, and summary.md
    to read. Compare two runs with Compare-UpgradeTestRuns.ps1.

    The workflows run as the application user the script connects as, so that user is the "initiating user" (07),
    sends the emails (09) and gets the user settings (07). That's the same in every run, so runs compare.

.PARAMETER Label
    A name for the run, e.g. before or after. Default the installed workflow tools version.

.PARAMETER User
    Passed to New-UpgradeTestData.ps1: your user (email address or user name).

.PARAMETER SkipTestData
    Doesn't run New-UpgradeTestData.ps1 first.

.PARAMETER Workflow
    Runs only these workflows, by number (e.g. 04, 11b) or name.

.PARAMETER OutputFolder
    Where the run's folder is created. Default testing\results.

.PARAMETER TimeoutMinutes
    How long to wait for each system job. Default 10.

.PARAMETER ConnectionVariable
    The environment variable with the connection string. Default DATAVERSE_CONNECTION (the Dynamics 365 test environment).

.EXAMPLE
    .\tools\Invoke-UpgradeTestRun.ps1 -Label before -User someone@contoso.com

.EXAMPLE
    .\tools\Invoke-UpgradeTestRun.ps1 -Workflow 04, 11b -SkipTestData
#>
[CmdletBinding()]
param(
    [string]$Label,
    [string]$User,
    [switch]$SkipTestData,
    [string[]]$Workflow,
    [string]$OutputFolder = (Join-Path $PSScriptRoot '..\testing\results'),
    [int]$TimeoutMinutes = 10,
    [string]$ConnectionVariable = 'DATAVERSE_CONNECTION'
)

$ErrorActionPreference = 'Stop'

if (-not $SkipTestData) {
    & (Join-Path $PSScriptRoot 'New-UpgradeTestData.ps1') -User $User -ConnectionVariable $ConnectionVariable
    Write-Host ''
}

# connection and Web API helpers
. (Join-Path $PSScriptRoot 'Dataverse.ps1')

# ---- helpers -------------------------------------------------------------------------------------------------------

function Get-First([string]$entitySet, [string]$query) {
    $records = Get-Records $entitySet "$query&`$top=1"

    if ($records.Count -eq 0) {
        return $null
    }

    return $records[0]
}

function Get-Required([string]$what, [string]$entitySet, [string]$query) {
    $record = Get-First $entitySet $query

    if (-not $record) {
        throw "$what not found. Run tools\New-UpgradeTestData.ps1 first."
    }

    return $record
}

function Get-UtcText([datetime]$time) {
    return $time.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
}

# a check's value as text: lists are joined, empty values shown as (empty)
function Format-Value($value) {
    if ($value -is [array]) {
        if ($value.Count -eq 0) {
            return '(none)'
        }

        return ($value | ForEach-Object { "$_" }) -join '; '
    }

    if ($null -eq $value -or "$value" -eq '') {
        return '(empty)'
    }

    if ($value -is [datetime]) {
        return $value.ToUniversalTime().ToString('o')
    }

    return "$value"
}

# a note's text as plain lines: the notes the workflows write use <br/> for new lines
function ConvertFrom-NoteText([string]$text) {
    if (-not $text) {
        return $text
    }

    $text = $text -replace '<br\s*/?>', "`n"

    return [Net.WebUtility]::HtmlDecode($text).TrimEnd()
}

# Deletes the qualified WFT Lead Qualify leads and what qualifying them created: duplicate detection (same email,
# same account name) would stop the next run qualifying
function Remove-QualifiedRecords {
    foreach ($lead in Get-Records 'leads' "`$select=leadid&`$filter=emailaddress1 eq 'wft-qualify@example.com' and statecode ne 0") {
        foreach ($set in @(@('opportunities', 'opportunityid'), @('accounts', 'accountid'), @('contacts', 'contactid'))) {
            foreach ($created in Get-Records $set[0] "`$select=$($set[1])&`$filter=_originatingleadid_value eq $($lead.leadid)") {
                Invoke-Api DELETE "$($set[0])($($created.($set[1])))" | Out-Null
            }
        }

        Invoke-Api DELETE "leads($($lead.leadid))" | Out-Null
    }
}

$jobStatus = @{ 0 = 'Waiting for resources'; 10 = 'Waiting'; 20 = 'In progress'; 21 = 'Pausing'; 22 = 'Canceling'; 30 = 'Succeeded'; 31 = 'Failed'; 32 = 'Canceled' }

# ---- the test records ----------------------------------------------------------------------------------------------

$assembly = Get-Required 'The workflow tools' 'pluginassemblies' "`$select=version&`$filter=name eq 'msdyncrmWorkflowTools'"

if (-not $Label) {
    $Label = $assembly.version
}

$me = Invoke-Api GET 'WhoAmI'
$account = Get-Required 'WFT Upgrade Account' 'accounts' "`$select=accountid,name&`$filter=name eq 'WFT Upgrade Account'"
$contact1 = Get-Required 'WFT Contact 1' 'contacts' "`$select=contactid,fullname&`$filter=fullname eq 'WFT Contact 1' and _parentcustomerid_value eq $($account.accountid)"
$team = Get-Required 'WFT Team' 'teams' "`$select=teamid&`$filter=name eq 'WFT Team'"
$queue = Get-Required 'WFT Queue' 'queues' "`$select=queueid&`$filter=name eq 'WFT Queue'"
$lists = @{}

foreach ($name in 'WFT List', 'WFT List 2', 'WFT Dynamic List') {
    $lists[$name] = (Get-Required $name 'lists' "`$select=listid&`$filter=listname eq '$name'").listid
}

$campaign = Get-Required 'WFT Campaign' 'campaigns' "`$select=campaignid&`$filter=name eq 'WFT Campaign'"
$opportunity = Get-Required 'WFT Opportunity' 'opportunities' "`$select=opportunityid&`$filter=name eq 'WFT Opportunity'"
$goal = Get-Required 'WFT Goal' 'goals' "`$select=goalid&`$filter=title eq 'WFT Goal'"
$bpf = Get-Required 'WFT Test BPF' 'workflows' "`$select=uniquename&`$filter=name eq 'WFT Test BPF' and type eq 1 and category eq 4"
$bpfSet = (Invoke-Api GET "EntityDefinitions(LogicalName='$($bpf.uniquename)')?`$select=EntitySetName").EntitySetName
$choiceTwo = Get-OptionValue 'account' 'new_wfttestchoice' 'PicklistAttributeMetadata' 'Two'
$choiceOne = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'One'
$choiceThree = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'Three'

# the records the workflows use up: the newest active one
$scratch = Get-Required 'An active WFT Scratch Account' 'accounts' "`$select=accountid,name&`$filter=name eq 'WFT Scratch Account' and statecode eq 0&`$orderby=createdon desc"
$qualifyLead = Get-Required 'An open WFT Lead Qualify' 'leads' "`$select=leadid,fullname&`$filter=emailaddress1 eq 'wft-qualify@example.com' and statecode eq 0&`$orderby=createdon desc"
$case = Get-Required 'An active WFT Case' 'incidents' "`$select=incidentid,title&`$filter=title eq 'WFT Case' and statecode eq 0&`$orderby=createdon desc"

$records = @{
    account     = @{ entity = 'account'; id = $account.accountid; name = $account.name }
    scratch     = @{ entity = 'account'; id = $scratch.accountid; name = $scratch.name }
    contact     = @{ entity = 'contact'; id = $contact1.contactid; name = $contact1.fullname }
    qualifyLead = @{ entity = 'lead'; id = $qualifyLead.leadid; name = $qualifyLead.fullname }
    case        = @{ entity = 'incident'; id = $case.incidentid; name = $case.title }
}

# ---- what each workflow did, beyond its log note -------------------------------------------------------------------

# WFT Test (run by Execute Workflow By ID) sets the account's description to 1; its system job is deleted when it succeeds
$wftTestRan = {
    (Invoke-Api GET "accounts($($account.accountid))?`$select=description").description
}

# put back before the step what it changes, when another step changes it too
$beforeStep = @{
    'WFT 03 Records'              = { Invoke-Api PATCH "accounts($($account.accountid))" @{ description = 'wft upgrade test' } | Out-Null }
    'WFT 10 Processes and queues' = { Invoke-Api PATCH "accounts($($account.accountid))" @{ description = 'wft upgrade test' } | Out-Null }
}

$checks = @{
    'WFT 03 Records' = {
        param($since)
        [ordered]@{ 'Account description (WFT Test sets 1)' = & $wftTestRan }
    }
    'WFT 03b Clone and child records' = {
        param($since)
        $result = [ordered]@{}

        foreach ($contact in Get-Records 'contacts' "`$select=fullname,description,telephone2,statecode&`$filter=_parentcustomerid_value eq $($account.accountid)&`$orderby=fullname") {
            $result["$($contact.fullname) description"] = $contact.description
            $result["$($contact.fullname) telephone2"] = $contact.telephone2
        }

        $result['Cloned accounts left'] = (Get-Records 'accounts' "`$select=accountid&`$filter=startswith(name,'COPY WFT Upgrade Account')").Count
        $cloned = Get-Records 'contacts' "`$select=fullname,_parentcustomerid_value&`$filter=startswith(fullname,'WFT Contact') and createdon ge $since&`$orderby=fullname"
        $result['Cloned contacts'] = @($cloned | ForEach-Object { $_.fullname })
        $result
    }
    'WFT 04 Status' = {
        param($since)
        $record = Invoke-Api GET "accounts($($scratch.accountid))?`$select=statecode,statuscode"
        [ordered]@{ 'Scratch account state' = $record.statecode; 'Scratch account status' = $record.statuscode }
    }
    'WFT 05 Option sets' = {
        param($since)
        $record = Invoke-Api GET "accounts($($account.accountid))?`$select=new_wfttestchoice,new_wfttestchoices"
        $options = (Invoke-Api GET "EntityDefinitions(LogicalName='account')/Attributes(LogicalName='new_wfttestchoice')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?`$select=LogicalName&`$expand=OptionSet(`$select=Options)").OptionSet.Options
        [ordered]@{
            'new_wfttestchoice'       = $record.new_wfttestchoice
            'new_wfttestchoices'      = $record.new_wfttestchoices
            'Option 100000900 exists' = [bool]($options | Where-Object { $_.Value -eq 100000900 })
        }
    }
    'WFT 07 Users, teams and roles' = {
        param($since)
        $result = [ordered]@{}
        $created = Get-First 'teams' "`$select=teamid,teamtype,_administratorid_value&`$filter=name eq 'WFT Created Team'"
        $result['WFT Created Team exists'] = [bool]$created

        if ($created) {
            $result['WFT Created Team type'] = $created.teamtype
            $result['WFT Created Team administrator is the initiating user'] = $created._administratorid_value -eq $me.UserId
            $result['WFT Created Team members'] = @((Get-Records "teams($($created.teamid))/teammembership_association" '$select=fullname') | ForEach-Object { $_.fullname } | Sort-Object)
        }

        $settings = Invoke-Api GET "usersettingscollection($($me.UserId))?`$select=paginglimit,advancedfindstartupmode,defaultcalendarview,issendasallowed"
        $result['Paging limit'] = $settings.paginglimit
        $result['Advanced Find startup mode'] = $settings.advancedfindstartupmode
        $result['Default calendar view'] = $settings.defaultcalendarview
        $result['Send As allowed'] = $settings.issendasallowed
        $result
    }
    'WFT 09 Email' = {
        param($since)
        $result = [ordered]@{}

        foreach ($email in Get-Records 'emails' "`$select=activityid,subject,statuscode,_regardingobjectid_value&`$filter=createdon ge $since and (startswith(subject,'WFT'))&`$orderby=subject") {
            $files = @((Get-Records 'activitymimeattachments' "`$select=filename&`$filter=_objectid_value eq $($email.activityid)") | ForEach-Object { $_.filename } | Sort-Object)
            $recipients = @((Get-Records 'activityparties' "`$select=participationtypemask,addressused,_partyid_value&`$filter=_activityid_value eq $($email.activityid) and participationtypemask eq 2") |
                ForEach-Object { if ($_.addressused) { $_.addressused } else { $_.'_partyid_value@OData.Community.Display.V1.FormattedValue' } } | Sort-Object)
            $key = $email.subject
            $n = 2

            while ($result.Contains("$key status")) {
                $key = "$($email.subject) ($n)"
                $n++
            }

            $result["$key status"] = $email.'statuscode@OData.Community.Display.V1.FormattedValue'
            $result["$key attachments"] = $files
            $result["$key to"] = $recipients
        }

        $result
    }
    'WFT 10 Processes and queues' = {
        param($since)
        $result = [ordered]@{}
        $instance = Get-First $bpfSet "`$select=_activestageid_value&`$filter=_bpf_contactid_value eq $($contact1.contactid)&`$orderby=modifiedon desc"

        if ($instance -and $instance._activestageid_value) {
            $result['BPF active stage'] = (Invoke-Api GET "processstages($($instance._activestageid_value))?`$select=stagename").stagename
        }
        else {
            $result['BPF active stage'] = $null
        }

        $result['Account description (WFT Test sets 1)'] = & $wftTestRan
        $result['Queue items picked'] = @((Get-Records 'queueitems' "`$select=title&`$filter=_queueid_value eq $($queue.queueid) and _workerid_value ne null&`$orderby=title") | ForEach-Object { $_.title })
        $result
    }
    'WFT 11a Sales and marketing' = {
        param($since)
        $result = [ordered]@{}

        foreach ($name in 'WFT List', 'WFT List 2') {
            $result["Account in $name"] = (Get-Records 'listmembers' "`$select=listmemberid&`$filter=_listid_value eq $($lists[$name]) and _entityid_value eq $($account.accountid)").Count -gt 0
        }

        $result['Campaign lists'] = @((Get-Records "campaigns($($campaign.campaignid))/campaignlist_association" '$select=listname') | ForEach-Object { $_.listname } | Sort-Object)
        # the copy of WFT Dynamic List should have every account its query (name begins with WFT) matches; the number
        # grows by a scratch account each run, so it's compared with the query instead of shown
        $matching = (Get-Records 'accounts' "`$select=accountid&`$filter=startswith(name,'WFT')").Count
        $result['New static lists'] = @((Get-Records 'lists' "`$select=listname,membercount&`$filter=createdon ge $since&`$orderby=listname") |
            ForEach-Object { "$($_.listname) (all $(if ($_.membercount -eq $matching) { '' } else { "but $($matching - $_.membercount) of the " })matching accounts)" })
        $price = Invoke-Api GET "opportunities($($opportunity.opportunityid))?`$select=totallineitemamount,totalamount"
        $result['Opportunity line items total'] = $price.totallineitemamount
        $result['Opportunity total'] = $price.totalamount
        $goalRecord = Invoke-Api GET "goals($($goal.goalid))?`$select=actualmoney,actualinteger,actualdecimal,percentage"
        $result['Goal actual'] = "$($goalRecord.actualmoney) $($goalRecord.actualinteger) $($goalRecord.actualdecimal)".Trim()
        $result['Goal percentage'] = $goalRecord.percentage
        $result
    }
    'WFT 11b Qualify lead' = {
        param($since)
        $lead = Invoke-Api GET "leads($($qualifyLead.leadid))?`$select=statecode,statuscode"
        $result = [ordered]@{ 'Lead state' = $lead.'statecode@OData.Community.Display.V1.FormattedValue'; 'Lead status' = $lead.'statuscode@OData.Community.Display.V1.FormattedValue' }

        foreach ($set in @(@('accounts', 'name'), @('contacts', 'fullname'), @('opportunities', 'name'))) {
            $result["Created $($set[0])"] = @((Get-Records $set[0] "`$select=$($set[1])&`$filter=_originatingleadid_value eq $($qualifyLead.leadid)") | ForEach-Object { $_.($set[1]) })
        }

        $result
    }
    'WFT 11c Case' = {
        param($since)
        $record = Invoke-Api GET "incidents($($case.incidentid))?`$select=statecode,statuscode"
        $resolution = Get-First 'incidentresolutions' "`$select=subject,description&`$filter=_incidentid_value eq $($case.incidentid)"
        [ordered]@{
            'Case state'             = $record.'statecode@OData.Community.Display.V1.FormattedValue'
            'Case status'            = $record.'statuscode@OData.Community.Display.V1.FormattedValue'
            'Resolution subject'     = $resolution.subject
            'Resolution description' = $resolution.description
        }
    }
}

# ---- the run order (05 before 04, which deactivates the scratch account) -------------------------------------------

$plan = @(
    @('WFT 01 Text and numbers', 'account'), @('WFT 02 Queries and rollups', 'account'), @('WFT 03 Records', 'account'),
    @('WFT 03b Clone and child records', 'account'), @('WFT 05 Option sets', 'account'), @('WFT 04 Status', 'scratch'),
    @('WFT 06 Relationships', 'account'), @('WFT 07 Users, teams and roles', 'account'), @('WFT 08 Sharing', 'account'),
    @('WFT 09 Email', 'account'), @('WFT 10 Processes and queues', 'contact'), @('WFT 11a Sales and marketing', 'account'),
    @('WFT 11b Qualify lead', 'qualifyLead'), @('WFT 11c Case', 'case'), @('WFT 12 Settings, apps and SharePoint', 'account'),
    @('WFT 13 External services', 'account')
)

if ($Workflow) {
    # pwsh -File passes "04,11b" as one value
    $Workflow = @($Workflow -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $plan = @($plan | Where-Object { $name = $_[0]; $Workflow | Where-Object { $name -eq $_ -or $name -like "WFT $_ *" } })

    if ($plan.Count -eq 0) {
        throw "No workflow matches $($Workflow -join ', ')."
    }
}

# ---- put back what earlier runs changed ----------------------------------------------------------------------------

Write-Host 'Resetting the test data'

foreach ($contact in Get-Records 'contacts' "`$select=contactid&`$filter=_parentcustomerid_value eq $($account.accountid)") {
    Invoke-Api PATCH "contacts($($contact.contactid))" @{ description = $null; telephone2 = $null } | Out-Null
}

Invoke-Api PATCH "accounts($($account.accountid))" @{ new_wfttestchoice = $choiceTwo; new_wfttestchoices = "$choiceOne,$choiceThree" } | Out-Null

foreach ($leftover in Get-Records 'teams' "`$select=teamid&`$filter=name eq 'WFT Created Team'") {
    Invoke-Api DELETE "teams($($leftover.teamid))" | Out-Null
}

Remove-QualifiedRecords

foreach ($item in Get-Records 'queueitems' "`$select=queueitemid&`$filter=_queueid_value eq $($queue.queueid) and _workerid_value ne null") {
    Invoke-Api POST "queueitems($($item.queueitemid))/Microsoft.Dynamics.CRM.ReleaseToQueue" @{} | Out-Null
}

# ---- run -----------------------------------------------------------------------------------------------------------

$definitions = @{}

foreach ($definition in Get-Records 'workflows' "`$select=workflowid,name&`$filter=startswith(name,'WFT ') and type eq 1 and category eq 0 and statecode eq 1") {
    $definitions[$definition.name] = $definition.workflowid
}

$runStarted = Get-Date
$results = [Collections.Generic.List[object]]::new()
# each note goes to the step that wrote it, not also to the next one on the same record
$collectedNotes = [Collections.Generic.HashSet[string]]::new()

foreach ($step in $plan) {
    $name, $recordKey = $step
    $record = $records[$recordKey]
    Write-Host "$name on $($record.name) ... " -NoNewline
    $result = [ordered]@{ name = $name; record = $record; status = $null; message = $null; seconds = $null; notes = @(); checks = [ordered]@{} }
    $results.Add($result)

    if (-not $definitions.ContainsKey($name)) {
        $result.status = 'Not activated'
        Write-Host $result.status -ForegroundColor Yellow
        continue
    }

    if ($beforeStep.ContainsKey($name)) {
        & $beforeStep[$name]
    }

    $started = (Get-Date).AddSeconds(-2)
    $since = Get-UtcText $started
    # the response is the system job (or, without return=representation, its id)
    $response = Invoke-Api POST "workflows($($definitions[$name]))/Microsoft.Dynamics.CRM.ExecuteWorkflow" @{ EntityId = $record.id }
    $jobId = if ($response.asyncoperationid) { $response.asyncoperationid } else { $response.Id }

    if (-not $jobId) {
        throw "ExecuteWorkflow for $name returned no system job."
    }

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)

    # until it completes, or is suspended (state 1): a classic workflow whose step fails waits there
    do {
        Start-Sleep -Seconds 3
        $job = Get-First 'asyncoperations' "`$select=statecode,statuscode,message,friendlymessage&`$filter=asyncoperationid eq $jobId"

        # the workflows delete their system job when it succeeds
        if (-not $job) {
            $job = [pscustomobject]@{ statecode = 3; statuscode = 30; message = $null; friendlymessage = $null }
        }
    } until ($job.statecode -eq 3 -or $job.statecode -eq 1 -or (Get-Date) -gt $deadline)

    # WFT workflows it started (Execute Workflow By ID runs WFT Test) finish too, so their results are in this step's window:
    # wait while one is ready (0) or running (2)
    $regarding = "_regardingobjectid_value eq $($record.id) or _regardingobjectid_value eq $($account.accountid)"
    $others = "createdon ge $since and operationtype eq 10 and startswith(name,'WFT') and asyncoperationid ne $jobId and (statecode eq 0 or statecode eq 2) and ($regarding)"

    while ((Get-Date) -lt $deadline -and (Get-Records 'asyncoperations' "`$select=asyncoperationid&`$filter=$others").Count -gt 0) {
        Start-Sleep -Seconds 3
    }

    $result.status = if ($job.statecode -eq 3) { $jobStatus[[int]$job.statuscode] } elseif ($job.statecode -eq 1) { "Suspended ($($jobStatus[[int]$job.statuscode]))" } else { "Timed out ($($jobStatus[[int]$job.statuscode]))" }
    $result.message = if ($job.friendlymessage) { $job.friendlymessage } else { $job.message }
    $result.seconds = [int]((Get-Date) - $started).TotalSeconds

    $result.notes = @(Get-Records 'annotations' "`$select=annotationid,subject,notetext,filename&`$filter=_objectid_value eq $($record.id) and createdon ge $since&`$orderby=createdon" |
        Where-Object { $collectedNotes.Add($_.annotationid) } |
        ForEach-Object { [ordered]@{ subject = $_.subject; text = ConvertFrom-NoteText $_.notetext; file = $_.filename } })

    if ($checks.ContainsKey($name)) {
        try {
            $values = & $checks[$name] $since

            foreach ($key in $values.Keys) {
                $result.checks[$key] = Format-Value $values[$key]
            }
        }
        catch {
            $result.checks['Check failed'] = "$_"
        }
    }

    $color = if ($result.status -eq 'Succeeded') { 'Green' } else { 'Red' }
    Write-Host "$($result.status) ($($result.seconds) s)" -ForegroundColor $color
}

# ---- clean up what the workflows leave behind ----------------------------------------------------------------------

$since = Get-UtcText $runStarted

foreach ($contact in Get-Records 'contacts' "`$select=contactid&`$filter=startswith(fullname,'WFT Contact') and createdon ge $since") {
    Invoke-Api DELETE "contacts($($contact.contactid))" | Out-Null
}

foreach ($leftover in Get-Records 'accounts' "`$select=accountid&`$filter=startswith(name,'COPY WFT Upgrade Account') and createdon ge $since") {
    Invoke-Api DELETE "accounts($($leftover.accountid))" | Out-Null
}

foreach ($leftover in Get-Records 'teams' "`$select=teamid&`$filter=name eq 'WFT Created Team'") {
    Invoke-Api DELETE "teams($($leftover.teamid))" | Out-Null
}

foreach ($leftover in Get-Records 'lists' "`$select=listid&`$filter=createdon ge $since") {
    Invoke-Api DELETE "lists($($leftover.listid))" | Out-Null
}

Remove-QualifiedRecords

# ---- save ----------------------------------------------------------------------------------------------------------

$folder = Join-Path $OutputFolder "$($runStarted.ToString('yyyy-MM-dd HHmm')) $Label"
New-Item -ItemType Directory -Force $folder | Out-Null

$run = [ordered]@{
    label         = $Label
    environment   = $url
    workflowTools = $assembly.version
    started       = $runStarted.ToUniversalTime().ToString('o')
    finished      = (Get-Date).ToUniversalTime().ToString('o')
    workflows     = $results
}

[IO.File]::WriteAllText((Join-Path $folder 'results.json'), ($run | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))

$summary = [Text.StringBuilder]::new()
[void]$summary.AppendLine("# Upgrade test run: $Label")
[void]$summary.AppendLine()
[void]$summary.AppendLine("Environment $url, workflow tools $($assembly.version), started $($runStarted.ToString('yyyy-MM-dd HH:mm')).")
[void]$summary.AppendLine()
[void]$summary.AppendLine('| Workflow | Record | Result | Seconds | Log notes |')
[void]$summary.AppendLine('| --- | --- | --- | --- | --- |')

foreach ($result in $results) {
    [void]$summary.AppendLine("| $($result.name) | $($result.record.name) | $($result.status) | $($result.seconds) | $($result.notes.Count) |")
}

foreach ($result in $results) {
    [void]$summary.AppendLine()
    [void]$summary.AppendLine("## $($result.name)")
    [void]$summary.AppendLine()
    [void]$summary.AppendLine("**$($result.status)** on $($result.record.name).")

    if ($result.message) {
        [void]$summary.AppendLine()
        [void]$summary.AppendLine('```')
        [void]$summary.AppendLine($result.message.Trim())
        [void]$summary.AppendLine('```')
    }

    foreach ($note in $result.notes) {
        [void]$summary.AppendLine()
        $file = if ($note.file) { " (file $($note.file))" } else { '' }
        [void]$summary.AppendLine("Note **$($note.subject)**$file")

        if ($note.text) {
            [void]$summary.AppendLine()
            [void]$summary.AppendLine('```')
            [void]$summary.AppendLine($note.text.Trim())
            [void]$summary.AppendLine('```')
        }
    }

    if ($result.checks.Count -gt 0) {
        [void]$summary.AppendLine()

        foreach ($key in $result.checks.Keys) {
            [void]$summary.AppendLine("- $($key): $($result.checks[$key])")
        }
    }
}

[IO.File]::WriteAllText((Join-Path $folder 'summary.md'), $summary.ToString(), [Text.UTF8Encoding]::new($false))

$failed = @($results | Where-Object { $_.status -ne 'Succeeded' }).Count
Write-Host ''
Write-Host "$($results.Count - $failed) of $($results.Count) succeeded. Results: $((Resolve-Path $folder).Path)" -ForegroundColor Cyan
