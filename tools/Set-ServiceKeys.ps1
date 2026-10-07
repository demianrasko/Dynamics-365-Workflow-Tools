<#
.SYNOPSIS
    Saves the keys the live service tests use, as user environment variables.

.DESCRIPTION
    Prompts for (press Enter to keep a value that is already set, or to skip it):
      TRANSLATOR_KEY      Azure AI Translator: the resource's Keys and Endpoint > Key 1
      TRANSLATOR_REGION   Azure AI Translator: the resource's Location/Region (e.g. westus2); empty for a global resource
      AZURE_MAPS_KEY      Azure Maps account: Authentication > Primary Key

    Currency conversion (Frankfurter) needs no key.

    The values are stored for your Windows user (HKCU\Environment), in plain text, like any environment variable.
    Restart Visual Studio (and terminals) afterwards so the tests see them.

.EXAMPLE
    .\Set-ServiceKeys.ps1

.EXAMPLE
    .\Set-ServiceKeys.ps1 -Remove
    Deletes all three variables.
#>
[CmdletBinding()]
param(
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'

$keys = @(
    @{ Variable = 'TRANSLATOR_KEY'; Prompt = 'Azure AI Translator key'; Secret = $true },
    @{ Variable = 'TRANSLATOR_REGION'; Prompt = 'Azure AI Translator region (e.g. westus2; empty for global)'; Secret = $false },
    @{ Variable = 'AZURE_MAPS_KEY'; Prompt = 'Azure Maps primary key'; Secret = $true }
)

foreach ($key in $keys) {
    if ($Remove) {
        [Environment]::SetEnvironmentVariable($key.Variable, $null, 'User')
        Remove-Item "Env:$($key.Variable)" -ErrorAction SilentlyContinue
        Write-Host "Removed $($key.Variable)."
        continue
    }

    $existing = [Environment]::GetEnvironmentVariable($key.Variable, 'User')
    $hint = if ($existing) { ' (already set, Enter keeps it)' } else { ' (Enter skips it)' }

    if ($key.Secret) {
        $secure = Read-Host "$($key.Prompt)$hint" -AsSecureString
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)

        try {
            $value = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        }
    }
    else {
        $value = Read-Host "$($key.Prompt)$hint"
    }

    $value = $value.Trim()

    if (-not $value) {
        Write-Host "  $($key.Variable) left as it is."
        continue
    }

    [Environment]::SetEnvironmentVariable($key.Variable, $value, 'User')
    Set-Item "Env:$($key.Variable)" $value
    $value = $null
    Write-Host "  Saved $($key.Variable)." -ForegroundColor Green
}

if (-not $Remove) {
    Write-Host ''
    Write-Host 'Done. Restart Visual Studio (and any open terminals) so the tests see the new values.'
}
