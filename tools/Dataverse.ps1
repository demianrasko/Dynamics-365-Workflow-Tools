<#
.SYNOPSIS
    Connection and Web API helpers shared by the scripts in tools (dot-source it).

.DESCRIPTION
    Connects with the connection string in the environment variable named by $ConnectionVariable (saved by
    Set-DataverseTestConnections.ps1) and sets $url, $api and $headers. The client secret is only sent to Azure AD.
#>

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

    # network errors and throttling are retried a few times
    for ($attempt = 1; ; $attempt++) {
        try {
            $response = Invoke-WebRequest @arguments
            break
        }
        catch {
            $status = [int]$_.Exception.Response.StatusCode
            $transient = $null -eq $_.Exception.Response -or $status -in 429, 502, 503, 504

            if (-not $transient -or $attempt -ge 3) {
                throw "$method $path failed: $(Get-Error $_)"
            }

            Start-Sleep -Seconds (5 * $attempt)
        }
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

$typeCodes = @{}

# A record's URL in the form the Record URL (Dynamic) value has: workflow tools 1.0.61.1 reads the table from etc=,
# the table's type code, and needs it first.
function Get-RecordUrl([string]$table, $id) {
    if (-not $typeCodes.ContainsKey($table)) {
        $typeCodes[$table] = (Invoke-Api GET "EntityDefinitions(LogicalName='$table')?`$select=ObjectTypeCode").ObjectTypeCode
    }

    return "$url/main.aspx?etc=$($typeCodes[$table])&id=$id&pagetype=entityrecord"
}

function Get-OptionValue([string]$entity, [string]$column, [string]$type, [string]$label) {
    $metadata = Invoke-Api GET "EntityDefinitions(LogicalName='$entity')/Attributes(LogicalName='$column')/Microsoft.Dynamics.CRM.$type`?`$select=LogicalName&`$expand=OptionSet(`$select=Options)"
    $option = $metadata.OptionSet.Options | Where-Object { $_.Label.UserLocalizedLabel.Label -eq $label } | Select-Object -First 1

    if (-not $option) {
        throw "$entity.$column has no option '$label'. Run the integration tests once to create the test columns."
    }

    return $option.Value
}
