<#
.SYNOPSIS
    Checks that a Dataverse application user (app registration + client secret) can connect and what it can do.

.DESCRIPTION
    Runs these checks against one environment and prints PASS/FAIL for each:
      1. Gets a token for the app registration (client credentials).
      2. WhoAmI: the app is set up as an application user in the environment.
      3. The application user's record: name, business unit, enabled or disabled.
      4. Its security roles (direct and through teams).
      5. Read access to a few tables the workflow tools use.
      6. Optional (-WriteTest): creates and deletes a test account, to prove create and delete access.

    Nothing is changed unless -WriteTest is given. The secret is never printed.

    Connection details come from the parameters, else from environment variables, else a prompt:
      DATAVERSE_URL            https://yourorg.crm.dynamics.com
      DATAVERSE_CLIENT_ID      the app registration's Application (client) ID
      DATAVERSE_CLIENT_SECRET  the client secret
      DATAVERSE_TENANT_ID      optional; found from the environment URL when not given
    A connection string in DATAVERSE_CONNECTION (Url=...;ClientId=...;ClientSecret=...;TenantId=...) also works.

.EXAMPLE
    .\Test-DataverseAppUser.ps1 -EnvironmentUrl https://contoso-dev.crm.dynamics.com -ClientId 00000000-0000-0000-0000-000000000000

.EXAMPLE
    .\Test-DataverseAppUser.ps1 -WriteTest
#>
[CmdletBinding()]
param(
    [string]$EnvironmentUrl,
    [string]$ClientId,
    [SecureString]$ClientSecret,
    [string]$TenantId,
    [string[]]$Tables = @('accounts', 'contacts', 'systemusers', 'teams', 'roles', 'workflows', 'environmentvariabledefinitions', 'annotations'),
    [switch]$WriteTest
)

$ErrorActionPreference = 'Stop'
$script:failures = 0

function Write-Check([string]$name, [bool]$passed, [string]$detail) {
    if ($passed) {
        Write-Host ('  PASS  {0}' -f $name) -ForegroundColor Green
    }
    else {
        Write-Host ('  FAIL  {0}' -f $name) -ForegroundColor Red
        $script:failures++
    }

    if ($detail) {
        Write-Host ('        {0}' -f $detail)
    }
}

function Get-ErrorText($errorRecord) {
    $text = $errorRecord.ErrorDetails.Message

    if ($text) {
        try {
            $json = $text | ConvertFrom-Json

            if ($json.error.message) {
                return $json.error.message
            }

            if ($json.error_description) {
                return ($json.error_description -split "`r?`n")[0]
            }
        }
        catch {
        }

        return $text
    }

    return $errorRecord.Exception.Message
}

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

# ---- connection details
$connection = if ($env:DATAVERSE_CONNECTION) { ConvertFrom-ConnectionString $env:DATAVERSE_CONNECTION } else { @{} }

if (-not $EnvironmentUrl) { $EnvironmentUrl = if ($env:DATAVERSE_URL) { $env:DATAVERSE_URL } else { $connection['url'] } }
if (-not $ClientId) { $ClientId = if ($env:DATAVERSE_CLIENT_ID) { $env:DATAVERSE_CLIENT_ID } else { $connection['clientid'] } }
if (-not $TenantId) { $TenantId = if ($env:DATAVERSE_TENANT_ID) { $env:DATAVERSE_TENANT_ID } else { $connection['tenantid'] } }

if (-not $EnvironmentUrl) { $EnvironmentUrl = Read-Host 'Environment URL (https://yourorg.crm.dynamics.com)' }
if (-not $ClientId) { $ClientId = Read-Host 'Application (client) ID' }

$plainSecret = if ($ClientSecret) {
    [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($ClientSecret))
}
elseif ($env:DATAVERSE_CLIENT_SECRET) {
    $env:DATAVERSE_CLIENT_SECRET
}
elseif ($connection['clientsecret']) {
    $connection['clientsecret']
}
else {
    $secure = Read-Host 'Client secret' -AsSecureString
    [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

$EnvironmentUrl = $EnvironmentUrl.TrimEnd('/')
$api = "$EnvironmentUrl/api/data/v9.2"

Write-Host ''
Write-Host "Environment: $EnvironmentUrl"
Write-Host "Client ID:   $ClientId"
Write-Host ''

# ---- tenant: ask the environment which tenant it belongs to when none was given
if (-not $TenantId) {
    try {
        Invoke-WebRequest -Uri "$api/" -Method Get -UseBasicParsing | Out-Null
    }
    catch {
        $challenge = $_.Exception.Response.Headers['WWW-Authenticate']

        if (-not $challenge) {
            $challenge = ($_.Exception.Response.Headers | Where-Object { $_.Key -eq 'WWW-Authenticate' }).Value -join ' '
        }

        if ($challenge -match 'login\.microsoftonline\.com/([0-9a-fA-F-]{36})') {
            $TenantId = $Matches[1]
        }
    }

    if (-not $TenantId) {
        Write-Check 'Find the tenant' $false 'Could not read the tenant from the environment URL. Check the URL, or pass -TenantId.'
        exit 1
    }
}

Write-Host "Tenant:      $TenantId"
Write-Host ''
Write-Host 'Checks'

# ---- 1. token
try {
    $token = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token" -Body @{
        grant_type    = 'client_credentials'
        client_id     = $ClientId
        client_secret = $plainSecret
        scope         = "$EnvironmentUrl/.default"
    }

    $plainSecret = $null
    Write-Check 'Get a token' $true ('Expires in {0} minutes.' -f [int]($token.expires_in / 60))
}
catch {
    Write-Check 'Get a token' $false (Get-ErrorText $_)
    Write-Host '        AADSTS7000215 = wrong secret, AADSTS700016 = wrong client ID or tenant, AADSTS7000222 = secret expired.'
    exit 1
}

$headers = @{
    Authorization      = "Bearer $($token.access_token)"
    Accept             = 'application/json'
    'OData-Version'    = '4.0'
    'OData-MaxVersion' = '4.0'
}

# ---- 2. WhoAmI
try {
    $whoAmI = Invoke-RestMethod -Uri "$api/WhoAmI" -Headers $headers
    Write-Check 'WhoAmI (the app is an application user here)' $true "User ID $($whoAmI.UserId)"
}
catch {
    Write-Check 'WhoAmI (the app is an application user here)' $false (Get-ErrorText $_)
    Write-Host '        Add the app in Power Platform admin center > Environments > (environment) > Settings > Application users.'
    exit 1
}

# ---- 3. the application user
try {
    $user = Invoke-RestMethod -Headers $headers -Uri ("$api/systemusers($($whoAmI.UserId))" +
        '?$select=fullname,applicationid,isdisabled,accessmode&$expand=businessunitid($select=name)')
    $state = if ($user.isdisabled) { 'DISABLED' } else { 'enabled' }
    Write-Check 'Application user record' (-not $user.isdisabled) "$($user.fullname), business unit '$($user.businessunitid.name)', $state"
}
catch {
    Write-Check 'Application user record' $false (Get-ErrorText $_)
}

# ---- 4. security roles
try {
    $direct = (Invoke-RestMethod -Headers $headers -Uri "$api/systemusers($($whoAmI.UserId))/systemuserroles_association?`$select=name").value
    $teams = (Invoke-RestMethod -Headers $headers -Uri ("$api/systemusers($($whoAmI.UserId))/teammembership_association" +
        '?$select=name&$expand=teamroles_association($select=name)')).value
    $teamRoles = @($teams | ForEach-Object { $team = $_.name; $_.teamroles_association | ForEach-Object { "$($_.name) (team $team)" } })
    $allRoles = @($direct | ForEach-Object { $_.name }) + $teamRoles

    $detail = if ($allRoles.Count) { $allRoles -join ', ' } else { 'No security roles: assign one to the application user.' }
    Write-Check 'Security roles' ($allRoles.Count -gt 0) $detail
}
catch {
    Write-Check 'Security roles' $false (Get-ErrorText $_)
}

# ---- 5. read access
foreach ($table in $Tables) {
    try {
        $rows = (Invoke-RestMethod -Headers $headers -Uri "$api/$table`?`$top=1").value
        Write-Check "Read $table" $true ('{0} row(s) returned (top 1).' -f @($rows).Count)
    }
    catch {
        Write-Check "Read $table" $false (Get-ErrorText $_)
    }
}

# ---- 6. create and delete (only with -WriteTest)
if ($WriteTest) {
    $id = $null

    try {
        $body = @{ name = "Workflow Tools access test $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" } | ConvertTo-Json
        $response = Invoke-WebRequest -Method Post -Uri "$api/accounts" -Headers $headers -Body $body -ContentType 'application/json' -UseBasicParsing
        $location = @($response.Headers['OData-EntityId'])[0]

        if ($location -match '\(([0-9a-fA-F-]{36})\)') {
            $id = $Matches[1]
        }

        Write-Check 'Create an account' $true "Account $id"
    }
    catch {
        Write-Check 'Create an account' $false (Get-ErrorText $_)
    }

    if ($id) {
        try {
            Invoke-RestMethod -Method Delete -Uri "$api/accounts($id)" -Headers $headers | Out-Null
            Write-Check 'Delete the account' $true
        }
        catch {
            Write-Check 'Delete the account' $false "$(Get-ErrorText $_) The test account $id was left behind."
        }
    }
}
else {
    Write-Host '  SKIP  Create and delete (run with -WriteTest to check write access)' -ForegroundColor DarkGray
}

Write-Host ''

if ($script:failures -eq 0) {
    Write-Host 'All checks passed.' -ForegroundColor Green
    exit 0
}

Write-Host "$script:failures check(s) failed." -ForegroundColor Red
exit 1
