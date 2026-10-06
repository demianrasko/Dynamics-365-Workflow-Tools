<#
.SYNOPSIS
    Creates the test data for the upgrade test workflows (testing\Upgrade test workflows.md).

.DESCRIPTION
    Creates every WFT record the upgrade test workflows use, in the Dynamics 365 test environment
    (DATAVERSE_CONNECTION, saved by Set-DataverseTestConnections.ps1). Safe to run again: each record is looked up
    by name first and only created when it's missing. The records the workflows use up (WFT Scratch Account, WFT Case,
    WFT Lead Qualify) are created again when there's no active one left, so run it before each test run.

    "You" in the test plan (the WFT Team member, the only holder of WFT Role, the goal owner) is the user given with
    -User. Without -User it's the application user the script connects as.

    It creates data only: no solutions, assemblies or workflows.

.PARAMETER User
    Your user in the environment: email address or user name (e.g. someone@contoso.com).

.PARAMETER ConnectionVariable
    The environment variable with the connection string. Default DATAVERSE_CONNECTION (the Dynamics 365 test environment).

.EXAMPLE
    .\tools\New-UpgradeTestData.ps1 -User someone@contoso.com
#>
[CmdletBinding()]
param(
    [string]$User,
    [string]$ConnectionVariable = 'DATAVERSE_CONNECTION'
)

$ErrorActionPreference = 'Stop'

# connection and Web API helpers
. (Join-Path $PSScriptRoot 'Dataverse.ps1')

$created = [Collections.Generic.List[string]]::new()
$kept = [Collections.Generic.List[string]]::new()

# Finds a record by a column value, or creates it; returns its id.
function Find-OrCreate([string]$what, [string]$entitySet, [string]$idColumn, [string]$filter, [hashtable]$body) {
    $existing = Get-Records $entitySet "`$select=$idColumn&`$filter=$filter&`$top=1"

    if ($existing.Count -gt 0) {
        $kept.Add($what)
        return $existing[0].$idColumn
    }

    $id = Invoke-Api POST $entitySet $body
    $created.Add($what)

    return $id
}

function Bind([string]$entitySet, [string]$id) {
    return "/$entitySet($id)"
}

function New-File([string]$name, [string]$text) {
    return [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($text))
}

# ---- who "you" are -------------------------------------------------------------------------------------------------

$whoAmI = Invoke-Api GET 'WhoAmI'

if ($User) {
    $escaped = Escape-OData $User
    $users = Get-Records 'systemusers' "`$select=systemuserid,fullname,_businessunitid_value&`$filter=internalemailaddress eq '$escaped' or domainname eq '$escaped'"

    if ($users.Count -ne 1) {
        throw "Found $($users.Count) users with the email or user name '$User'."
    }

    $me = $users[0]
}
else {
    $me = Invoke-Api GET "systemusers($($whoAmI.UserId))?`$select=systemuserid,fullname,_businessunitid_value"
    Write-Warning 'No -User given: the application user is used as "you" (team member, WFT Role holder, goal owner).'
}

Write-Host "You:         $($me.fullname)"
$meId = $me.systemuserid
$rootBusinessUnit = (Get-Records 'businessunits' "`$select=businessunitid&`$filter=_parentbusinessunitid_value eq null")[0].businessunitid
$baseCurrency = (Get-Records 'organizations' "`$select=_basecurrencyid_value")[0]._basecurrencyid_value
Write-Host ''

# ---- leads ---------------------------------------------------------------------------------------------------------

# leads are found by email: this environment's own customizations may change a new lead's topic
$lead = Find-OrCreate 'Lead WFT Lead' 'leads' 'leadid' "emailaddress1 eq 'wft-lead@example.com'" @{
    subject = 'WFT Lead'; firstname = 'WFT'; lastname = 'Lead'; companyname = 'WFT Lead Company'; emailaddress1 = 'wft-lead@example.com'
}

Find-OrCreate 'Lead WFT Lead Qualify (open)' 'leads' 'leadid' "emailaddress1 eq 'wft-qualify@example.com' and statecode eq 0" @{
    subject = 'WFT Lead Qualify'; firstname = 'WFT'; lastname = 'Lead Qualify'; companyname = 'WFT Qualify Company'; emailaddress1 = 'wft-qualify@example.com'
} | Out-Null

# ---- accounts and contacts -----------------------------------------------------------------------------------------

$choice = Get-OptionValue 'account' 'new_wfttestchoice' 'PicklistAttributeMetadata' 'Two'
$choiceOne = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'One'
$choiceThree = Get-OptionValue 'account' 'new_wfttestchoices' 'MultiSelectPicklistAttributeMetadata' 'Three'

$account = Find-OrCreate 'Account WFT Upgrade Account' 'accounts' 'accountid' "name eq 'WFT Upgrade Account'" @{
    name                     = 'WFT Upgrade Account'
    accountnumber            = 'WFT-001'
    industrycode             = 1
    description              = 'wft upgrade test'
    creditlimit              = 1000
    telephone1               = '555-0100'
    address1_line1           = '1 Microsoft Way'
    address1_city            = 'Redmond'
    address1_stateorprovince = 'WA'
    address1_postalcode      = '98052'
    address1_country         = 'USA'
    new_wfttestchoice        = $choice
    new_wfttestchoices       = "$choiceOne,$choiceThree"
    # can only be set when the account is created
    'originatingleadid@odata.bind' = Bind 'leads' $lead
}

$contacts = foreach ($n in 1..3) {
    Find-OrCreate "Contact WFT Contact $n" 'contacts' 'contactid' "firstname eq 'WFT' and lastname eq 'Contact $n'" @{
        firstname                        = 'WFT'
        lastname                         = "Contact $n"
        emailaddress1                    = "wft-$n@example.com"
        numberofchildren                 = $n
        'parentcustomerid_account@odata.bind' = Bind 'accounts' $account
    }
}

# WFT Contact 3 is inactive (for "update only active")
Invoke-Api PATCH "contacts($($contacts[2]))" @{ statecode = 1; statuscode = 2 } | Out-Null

Invoke-Api PATCH "accounts($account)" @{ 'primarycontactid@odata.bind' = Bind 'contacts' $contacts[0] } | Out-Null

$scratch = Find-OrCreate 'Account WFT Scratch Account (active)' 'accounts' 'accountid' "name eq 'WFT Scratch Account' and statecode eq 0" @{
    name = 'WFT Scratch Account'
}

# ---- notes with attachments ----------------------------------------------------------------------------------------

foreach ($file in @(
        @{ Name = 'wft-a.txt'; Type = 'text/plain'; Body = New-File 'wft-a.txt' 'WFT upgrade test attachment A' },
        @{ Name = 'wft-b.pdf'; Type = 'application/pdf'; Body = New-File 'wft-b.pdf' "%PDF-1.1`n% WFT upgrade test attachment B`n%%EOF" })) {
    Find-OrCreate "Note $($file.Name) on WFT Upgrade Account" 'annotations' 'annotationid' "filename eq '$($file.Name)' and _objectid_value eq $account" @{
        subject                       = "WFT $($file.Name)"
        filename                      = $file.Name
        mimetype                      = $file.Type
        documentbody                  = $file.Body
        'objectid_account@odata.bind' = Bind 'accounts' $account
    } | Out-Null
}

# ---- opportunity with a product line -------------------------------------------------------------------------------

$unit = (Get-Records 'uoms' "`$select=uomid,_uomscheduleid_value&`$filter=isschedulebaseuom eq true&`$top=1")[0]

$priceList = Find-OrCreate 'Price list WFT Price List' 'pricelevels' 'pricelevelid' "name eq 'WFT Price List'" @{
    name                                = 'WFT Price List'
    'transactioncurrencyid@odata.bind' = Bind 'transactioncurrencies' $baseCurrency
}

$product = Find-OrCreate 'Product WFT Product' 'products' 'productid' "productnumber eq 'WFT-PRODUCT'" @{
    name                             = 'WFT Product'
    productnumber                    = 'WFT-PRODUCT'
    quantitydecimal                  = 2
    'defaultuomscheduleid@odata.bind' = Bind 'uomschedules' $unit._uomscheduleid_value
    'defaultuomid@odata.bind'        = Bind 'uoms' $unit.uomid
}

Find-OrCreate 'Price list item WFT Product' 'productpricelevels' 'productpricelevelid' "_productid_value eq $product and _pricelevelid_value eq $priceList" @{
    amount                    = 25
    'pricelevelid@odata.bind' = Bind 'pricelevels' $priceList
    'productid@odata.bind'    = Bind 'products' $product
    'uomid@odata.bind'        = Bind 'uoms' $unit.uomid
} | Out-Null

# a product created as a draft has to be published before it can be sold
if ((Invoke-Api GET "products($product)?`$select=statecode").statecode -eq 2) {
    Invoke-Api POST "products($product)/Microsoft.Dynamics.CRM.PublishProductHierarchy" @{} | Out-Null
}

try {
    $opportunity = Find-OrCreate 'Opportunity WFT Opportunity' 'opportunities' 'opportunityid' "name eq 'WFT Opportunity'" @{
        name                          = 'WFT Opportunity'
        'customerid_account@odata.bind' = Bind 'accounts' $account
        'parentaccountid@odata.bind'  = Bind 'accounts' $account
        'pricelevelid@odata.bind'     = Bind 'pricelevels' $priceList
    }

    Find-OrCreate 'Opportunity line WFT Product' 'opportunityproducts' 'opportunityproductid' "_opportunityid_value eq $opportunity" @{
        quantity                   = 2
        'opportunityid@odata.bind' = Bind 'opportunities' $opportunity
        'productid@odata.bind'     = Bind 'products' $product
        'uomid@odata.bind'         = Bind 'uoms' $unit.uomid
    } | Out-Null
}
catch {
    Write-Warning "The opportunity couldn't be created (a plug-in in this environment may block it): $_"
}

# ---- case ----------------------------------------------------------------------------------------------------------

Find-OrCreate 'Case WFT Case (active)' 'incidents' 'incidentid' "title eq 'WFT Case' and statecode eq 0" @{
    title                           = 'WFT Case'
    'customerid_account@odata.bind' = Bind 'accounts' $account
} | Out-Null

# ---- team and role -------------------------------------------------------------------------------------------------

$team = Find-OrCreate 'Team WFT Team' 'teams' 'teamid' "name eq 'WFT Team'" @{
    name                          = 'WFT Team'
    teamtype                      = 0
    'businessunitid@odata.bind'   = Bind 'businessunits' $rootBusinessUnit
    'administratorid@odata.bind'  = Bind 'systemusers' $meId
}

if ((Get-Records "systemusers($meId)/teammembership_association" "`$select=teamid&`$filter=teamid eq $team").Count -eq 0) {
    Invoke-Api POST "teams($team)/Microsoft.Dynamics.CRM.AddMembersTeam" @{
        Members = @(@{ '@odata.type' = 'Microsoft.Dynamics.CRM.systemuser'; systemuserid = $meId })
    } | Out-Null
    $created.Add("$($me.fullname) added to WFT Team")
}

Find-OrCreate 'Security role WFT Role' 'roles' 'roleid' "name eq 'WFT Role' and _businessunitid_value eq $rootBusinessUnit" @{
    name                        = 'WFT Role'
    'businessunitid@odata.bind' = Bind 'businessunits' $rootBusinessUnit
} | Out-Null

# a user holds the copy of the role in their own business unit
$myRole = (Get-Records 'roles' "`$select=roleid&`$filter=name eq 'WFT Role' and _businessunitid_value eq $($me._businessunitid_value)")[0].roleid

if ((Get-Records "systemusers($meId)/systemuserroles_association" "`$select=roleid&`$filter=roleid eq $myRole").Count -eq 0) {
    Invoke-Api POST "systemusers($meId)/systemuserroles_association/`$ref" @{ '@odata.id' = "$api/roles($myRole)" } | Out-Null
    $created.Add("WFT Role given to $($me.fullname)")
}

# ---- queue with two tasks ------------------------------------------------------------------------------------------

$queue = Find-OrCreate 'Queue WFT Queue' 'queues' 'queueid' "name eq 'WFT Queue'" @{ name = 'WFT Queue' }

foreach ($n in 1..2) {
    $task = Find-OrCreate "Task WFT Task $n" 'tasks' 'activityid' "subject eq 'WFT Task $n'" @{ subject = "WFT Task $n" }

    if ((Get-Records 'queueitems' "`$select=queueitemid&`$filter=_objectid_value eq $task and _queueid_value eq $queue").Count -eq 0) {
        Invoke-Api POST "queues($queue)/Microsoft.Dynamics.CRM.AddToQueue" @{
            Target = @{ '@odata.type' = 'Microsoft.Dynamics.CRM.task'; activityid = $task }
        } | Out-Null
        $created.Add("WFT Task $n added to WFT Queue")
    }
}

# ---- marketing lists and campaign ----------------------------------------------------------------------------------

foreach ($name in 'WFT List', 'WFT List 2') {
    Find-OrCreate "Marketing list $name" 'lists' 'listid' "listname eq '$name'" @{ listname = $name; createdfromcode = 1; type = $false } | Out-Null
}

Find-OrCreate 'Marketing list WFT Dynamic List' 'lists' 'listid' "listname eq 'WFT Dynamic List'" @{
    listname        = 'WFT Dynamic List'
    createdfromcode = 1
    type            = $true
    query           = '<fetch><entity name="account"><attribute name="accountid" /><filter><condition attribute="name" operator="like" value="WFT%" /></filter></entity></fetch>'
} | Out-Null

Find-OrCreate 'Campaign WFT Campaign' 'campaigns' 'campaignid' "name eq 'WFT Campaign'" @{ name = 'WFT Campaign' } | Out-Null

# ---- goal ----------------------------------------------------------------------------------------------------------

$metric = Find-OrCreate 'Goal metric WFT Metric' 'metrics' 'metricid' "name eq 'WFT Metric'" @{ name = 'WFT Metric'; amountdatatype = 0; isamount = $true }
$today = Get-Date
$monthStart = Get-Date -Year $today.Year -Month $today.Month -Day 1

Find-OrCreate 'Goal WFT Goal' 'goals' 'goalid' "title eq 'WFT Goal'" @{
    title                          = 'WFT Goal'
    'metricid@odata.bind'          = Bind 'metrics' $metric
    'goalownerid_systemuser@odata.bind' = Bind 'systemusers' $meId
    isfiscalperiodgoal             = $false
    goalstartdate                  = $monthStart.ToString('yyyy-MM-dd')
    goalenddate                    = $monthStart.AddMonths(1).AddDays(-1).ToString('yyyy-MM-dd')
    targetmoney                    = 100
} | Out-Null

# ---- sales literature ----------------------------------------------------------------------------------------------

$literature = Find-OrCreate 'Sales literature WFT Literature' 'salesliteratures' 'salesliteratureid' "name eq 'WFT Literature'" @{ name = 'WFT Literature' }

Find-OrCreate 'Sales literature item wft-brochure.txt' 'salesliteratureitems' 'salesliteratureitemid' "_salesliteratureid_value eq $literature and filename eq 'wft-brochure.txt'" @{
    title                         = 'WFT Brochure'
    filename                      = 'wft-brochure.txt'
    mimetype                      = 'text/plain'
    documentbody                  = New-File 'wft-brochure.txt' 'WFT upgrade test brochure'
    'salesliteratureid@odata.bind' = Bind 'salesliteratures' $literature
} | Out-Null

# ---- email template ------------------------------------------------------------------------------------------------

function New-Xsl([string]$text) {
    return '<?xml version="1.0" ?><xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"><xsl:output method="text" indent="no"/><xsl:template match="/data"><![CDATA[' + $text + ']]></xsl:template></xsl:stylesheet>'
}

Find-OrCreate 'Email template WFT Template' 'templates' 'templateid' "title eq 'WFT Template'" @{
    title            = 'WFT Template'
    templatetypecode = 'systemuser'
    languagecode     = 1033
    ispersonal       = $false
    subject          = New-Xsl 'WFT template test'
    body             = New-Xsl 'Sent by the WFT upgrade test workflows.'
} | Out-Null

# ---- SharePoint document location ----------------------------------------------------------------------------------

$site = Get-Records 'sharepointsites' '$select=sharepointsiteid,name&$top=1'

if ($site.Count -eq 0) {
    Write-Warning 'No SharePoint site in this environment: no document location created.'
}
else {
    Find-OrCreate 'SharePoint document location for WFT Upgrade Account' 'sharepointdocumentlocations' 'sharepointdocumentlocationid' "_regardingobjectid_value eq $account" @{
        name                                          = 'WFT Upgrade Account documents'
        relativeurl                                   = "WFT Upgrade Account_$($account.ToString().Replace('-', '').ToUpperInvariant())"
        'parentsiteorlocation_sharepointsite@odata.bind' = Bind 'sharepointsites' $site[0].sharepointsiteid
        'regardingobjectid_account@odata.bind'        = Bind 'accounts' $account
    } | Out-Null
}

# ---- what already has to be there ----------------------------------------------------------------------------------

$bpf = Get-Records 'workflows' "`$select=workflowid,statecode&`$filter=name eq 'WFT Test BPF' and type eq 1 and category eq 4"
$test = Get-Records 'workflows' "`$select=workflowid,statecode&`$filter=name eq 'WFT Test' and type eq 1 and category eq 0"

Write-Host ''
Write-Host "Created ($($created.Count)):" -ForegroundColor Green
$created | ForEach-Object { Write-Host "  $_" }
Write-Host "Already there ($($kept.Count)):"
$kept | ForEach-Object { Write-Host "  $_" }
Write-Host ''

if ($bpf.Count -eq 0) {
    Write-Warning 'WFT Test BPF (business process flow on contact) is missing.'
}
else {
    $stages = Get-Records 'processstages' "`$select=stagename&`$filter=_processid_value eq $($bpf[0].workflowid)" | ForEach-Object { $_.stagename }
    Write-Host "WFT Test BPF stages: $($stages -join ', ')"
}

if ($test.Count -eq 0) {
    Write-Warning 'The WFT Test workflow (on-demand, account) is missing.'
}

Write-Host ''
Write-Host 'For the steps that need a pasted value (see the test plan):' -ForegroundColor Cyan
Write-Host "  WFT Upgrade Account id:   $account"
Write-Host "  WFT Upgrade Account URL:  $(Get-RecordUrl 'account' $account)"
Write-Host "  WFT Scratch Account URL:  $(Get-RecordUrl 'account' $scratch)"
Write-Host "  WFT Lead URL:             $(Get-RecordUrl 'lead' $lead)"

if ($opportunity) {
    Write-Host "  WFT Opportunity URL:      $(Get-RecordUrl 'opportunity' $opportunity)"
}
