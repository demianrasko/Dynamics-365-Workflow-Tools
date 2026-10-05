<#
.SYNOPSIS
    Saves the connection strings the integration tests use, as user environment variables.

.DESCRIPTION
    Prompts for the Dynamics 365 and the Power Platform test environments and saves:
      DATAVERSE_CONNECTION     the Dynamics 365 environment (Sales etc. installed)
      DATAVERSE_CONNECTION_PP  the plain Dataverse (Power Platform) environment
    as AuthType=ClientSecret connection strings, which CrmServiceClient and Test-DataverseAppUser.ps1 both read.

    Each connection is checked with Test-DataverseAppUser.ps1 before it is saved (skip with -SkipCheck).
    Press Enter at the URL prompt to leave that environment's variable as it is.

    The variables are stored for your Windows user (HKCU\Environment), in plain text, like any environment
    variable. Use test environments only. Restart Visual Studio (and terminals) afterwards so they see the values.

.EXAMPLE
    .\Set-DataverseTestConnections.ps1

.EXAMPLE
    .\Set-DataverseTestConnections.ps1 -Remove
    Deletes both variables.
#>
[CmdletBinding()]
param(
    [switch]$SkipCheck,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'

$environments = @(
    @{ Variable = 'DATAVERSE_CONNECTION'; Name = 'Dynamics 365' },
    @{ Variable = 'DATAVERSE_CONNECTION_PP'; Name = 'Power Platform' }
)

if ($Remove) {
    foreach ($environment in $environments) {
        [Environment]::SetEnvironmentVariable($environment.Variable, $null, 'User')
        Remove-Item "Env:$($environment.Variable)" -ErrorAction SilentlyContinue
        Write-Host "Removed $($environment.Variable)."
    }

    return
}

function ConvertTo-PlainText([SecureString]$secure) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)

    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

$testScript = Join-Path $PSScriptRoot 'Test-DataverseAppUser.ps1'
$previous = $null

foreach ($environment in $environments) {
    Write-Host ''
    Write-Host "$($environment.Name) test environment ($($environment.Variable))" -ForegroundColor Cyan

    $existing = [Environment]::GetEnvironmentVariable($environment.Variable, 'User')

    if ($existing) {
        Write-Host '  Already set. Press Enter at the URL prompt to keep it.'
    }

    $url = (Read-Host '  Environment URL (https://yourorg.crm.dynamics.com)').Trim().TrimEnd('/')

    if (-not $url) {
        Write-Host "  $($environment.Variable) left as it is."
        continue
    }

    if ($url -notmatch '^https://') {
        $url = "https://$url"
    }

    $clientId = $null
    $secret = $null

    if ($previous -and (Read-Host '  Use the same app registration (client ID and secret) as above? [Y/n]') -notmatch '^[Nn]') {
        $clientId = $previous.ClientId
        $secret = $previous.Secret
    }
    else {
        $clientId = (Read-Host '  Application (client) ID').Trim()
        $secret = Read-Host '  Client secret' -AsSecureString
    }

    $tenantId = (Read-Host '  Tenant ID (optional, press Enter to find it from the URL)').Trim()

    if (-not $SkipCheck) {
        Write-Host '  Checking the connection...'
        $arguments = @{ EnvironmentUrl = $url; ClientId = $clientId; ClientSecret = $secret }

        if ($tenantId) {
            $arguments.TenantId = $tenantId
        }

        & $testScript @arguments

        if ($LASTEXITCODE -ne 0 -and (Read-Host '  The check failed. Save it anyway? [y/N]') -notmatch '^[Yy]') {
            Write-Host "  $($environment.Variable) not saved."
            continue
        }
    }

    $connection = "AuthType=ClientSecret;Url=$url;ClientId=$clientId;ClientSecret=$(ConvertTo-PlainText $secret)"

    if ($tenantId) {
        $connection += ";TenantId=$tenantId"
    }

    [Environment]::SetEnvironmentVariable($environment.Variable, $connection, 'User')
    Set-Item "Env:$($environment.Variable)" $connection
    $connection = $null

    Write-Host "  Saved $($environment.Variable)." -ForegroundColor Green
    $previous = @{ ClientId = $clientId; Secret = $secret }
}

Write-Host ''
Write-Host 'Done. Restart Visual Studio (and any open terminals) so the tests see the new values.'
