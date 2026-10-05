<#
.SYNOPSIS
    Builds the Dynamics 365 and Power Platform solution files (managed and unmanaged) from the built assemblies.

.DESCRIPTION
    For each solution in the solution folder (Dynamics365WorkflowTools.json, PowerPlatformWorkflowTools.json):
      1. Reads the workflow activities in the built assembly.
      2. Checks them against the solution's identity file, which keeps the ids the solution has always used, so an
         import upgrades the installed solution in place. Each activity is registered under the name in its
         [ActivityName("...")] attribute (designer name and friendly name); an activity without one stops the build. A new activity gets a new id, written back to the identity
         file (commit it). An activity that is in the identity file but no longer in the assembly stops the build,
         because removing it from the solution would break the workflows that use it (-AllowRemovedActivities to
         override).
      3. Writes the solution files and packs them with the Power Platform CLI (pac solution pack) into
         <OutputFolder>\<Name>_<version>.zip and <Name>_<version>_managed.zip.

    The solution version is the assembly version (Properties\AssemblyInfo.cs).

.EXAMPLE
    .\tools\Build-Solutions.ps1

.EXAMPLE
    .\tools\Build-Solutions.ps1 -SkipBuild
    Packs the assemblies that are already built.
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$OutputFolder,
    [string[]]$Solution,
    [switch]$AllowRemovedActivities
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'msdyncrmWorkflowTools'
$identityFolder = Join-Path $repo 'solution'

if (-not $OutputFolder) {
    $OutputFolder = Join-Path $repo 'dist'
}

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw 'The Power Platform CLI (pac) is needed: https://learn.microsoft.com/power-platform/developer/cli/introduction'
}

# ---- build both assemblies
if (-not $SkipBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1

    Write-Host 'Building the Dynamics 365 assembly...'
    & $msbuild (Join-Path $source 'msdyncrmWorkflowTools.sln') /t:Restore /p:RestorePackagesConfig=true /nologo /v:q
    & $msbuild (Join-Path $source 'msdyncrmWorkflowTools\msdyncrmWorkflowTools.csproj') /p:Configuration=Release /nologo /v:q
    if ($LASTEXITCODE -ne 0) { throw 'The Dynamics 365 build failed.' }

    Write-Host 'Building the Power Platform assembly...'
    & $msbuild (Join-Path $source 'msdyncrmWorkflowTools\msdyncrmWorkflowTools.csproj') /p:Configuration=Release /p:PowerPlatform=true /nologo /v:q
    if ($LASTEXITCODE -ne 0) { throw 'The Power Platform build failed.' }
}

# ---- the activities in an assembly (loaded in Windows PowerShell, which runs on .NET Framework like the assembly)
function Get-Activities([string]$dll) {
    $script = @'
param($dll)
$assembly = [Reflection.Assembly]::LoadFrom($dll)
try { $types = $assembly.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $types = $_.Exception.Types | Where-Object { $_ } }
"assembly`t$($assembly.FullName)"
foreach ($type in $types) {
    if (-not $type.IsPublic -or $type.IsAbstract) { continue }
    for ($base = $type.BaseType; $base; $base = $base.BaseType) {
        if ($base.FullName -eq 'System.Activities.CodeActivity') {
            $name = $type.GetCustomAttributesData() | Where-Object { $_.AttributeType.Name -eq 'ActivityNameAttribute' } | ForEach-Object { $_.ConstructorArguments[0].Value }
            "type`t$($type.FullName)`t$name"
            break
        }
    }
}
'@
    $file = Join-Path ([IO.Path]::GetTempPath()) 'wft-activities.ps1'
    Set-Content $file $script -Encoding UTF8
    $lines = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $file $dll

    if ($LASTEXITCODE -ne 0) {
        throw "Could not read the activities in $dll"
    }

    $names = @{}

    foreach ($line in $lines | Where-Object { $_ -like "type`t*" }) {
        $parts = $line.Split("`t")
        $names[$parts[1]] = if ($parts.Count -gt 2) { $parts[2] } else { '' }
    }

    return @{
        FullName = ($lines | Where-Object { $_ -like "assembly`t*" } | Select-Object -First 1).Split("`t")[1]
        Types    = @($names.Keys | Sort-Object)
        Names    = $names
    }
}

function Escape([string]$text) {
    return [Security.SecurityElement]::Escape($text)
}

$publisher = (Get-Content (Join-Path $identityFolder 'publisher.xml') -Raw).TrimEnd()
New-Item -ItemType Directory -Force $OutputFolder | Out-Null
$built = @()

foreach ($identityFile in Get-ChildItem $identityFolder -Filter *.json | Where-Object { -not $Solution -or $Solution -contains $_.BaseName } | Sort-Object Name) {
    $identity = Get-Content $identityFile.FullName -Raw | ConvertFrom-Json -AsHashtable
    $dll = Join-Path $source "msdyncrmWorkflowTools\bin\$($identity.buildFolder)\$($identity.assemblyName).dll"

    if (-not (Test-Path $dll)) {
        throw "$dll doesn't exist; build it first (or run without -SkipBuild)."
    }

    $assembly = Get-Activities $dll
    $version = [Reflection.AssemblyName]::new($assembly.FullName).Version.ToString()
    Write-Host ''
    Write-Host "$($identity.uniqueName) $version ($($assembly.Types.Count) activities)" -ForegroundColor Cyan

    $unnamed = @($assembly.Types | Where-Object { -not $assembly.Names[$_] })

    if ($unnamed.Count -gt 0) {
        throw "Add [ActivityName(`"...`")] (the name in the workflow designer) to: $($unnamed -join ', ')"
    }

    # ---- keep the ids stable
    $removed = @($identity.activities.Keys | Where-Object { $assembly.Types -notcontains $_ })

    if ($removed.Count -gt 0 -and -not $AllowRemovedActivities) {
        throw "These activities are in $($identityFile.Name) but not in the assembly, and removing them would break the workflows that use them: $($removed -join ', ')"
    }

    $added = @($assembly.Types | Where-Object { -not $identity.activities.ContainsKey($_) })

    foreach ($type in $added) {
        $identity.activities[$type] = [ordered]@{ id = [guid]::NewGuid().ToString() }
        Write-Host "  new activity: $type ($($assembly.Names[$type]))" -ForegroundColor Yellow
    }

    if ($added.Count -gt 0) {
        $sorted = [ordered]@{}
        foreach ($key in ($identity.activities.Keys | Sort-Object)) { $sorted[$key] = $identity.activities[$key] }
        $identity.activities = $sorted
        $identity | ConvertTo-Json -Depth 5 | Set-Content $identityFile.FullName -Encoding utf8
        Write-Host "  $($identityFile.Name) updated with the new ids: commit it." -ForegroundColor Yellow
    }

    # ---- the solution files
    $folder = Join-Path ([IO.Path]::GetTempPath()) "wftsolution\$($identity.uniqueName)"
    if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }

    $assemblyFolderName = "$($identity.assemblyName)-$(([guid]$identity.assemblyId).ToString().ToUpperInvariant())"
    $assemblyFolder = Join-Path $folder "PluginAssemblies\$assemblyFolderName"
    New-Item -ItemType Directory -Force (Join-Path $folder 'Other'), $assemblyFolder | Out-Null
    Copy-Item $dll (Join-Path $assemblyFolder "$($identity.assemblyName).dll")

    $group = "$($identity.assemblyName) ($version)"
    $types = foreach ($type in $assembly.Types) {
        $activity = $identity.activities[$type]
        $name = $assembly.Names[$type]
        @"
    <PluginType AssemblyQualifiedName="$type, $(Escape $assembly.FullName)" PluginTypeId="$($activity.id)" Name="$(Escape $name)">
      <FriendlyName>$(Escape $name)</FriendlyName>
      <WorkflowActivityGroupName>$(Escape $group)</WorkflowActivityGroupName>
    </PluginType>
"@
    }

    @"
<?xml version="1.0" encoding="utf-8"?>
<PluginAssembly FullName="$(Escape $assembly.FullName)" PluginAssemblyId="$($identity.assemblyId)" CustomizationLevel="1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <IsolationMode>2</IsolationMode>
  <SourceType>0</SourceType>
  <IntroducedVersion>1.0</IntroducedVersion>
  <FileName>/PluginAssemblies/$assemblyFolderName/$($identity.assemblyName).dll</FileName>
  <PluginTypes>
$($types -join "`n")
  </PluginTypes>
</PluginAssembly>
"@ | Set-Content (Join-Path $assemblyFolder "$($identity.assemblyName).dll.data.xml") -Encoding utf8

    @"
<?xml version="1.0" encoding="utf-8"?>
<ImportExportXml version="9.2.26094.133" SolutionPackageVersion="9.2" languagecode="1033" generatedBy="CrmLive" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" OrganizationVersion="9.2.26094.133" OrganizationSchemaType="Standard" CRMServerServiceabilityVersion="9.2.26094.00133">
  <SolutionManifest>
    <UniqueName>$($identity.uniqueName)</UniqueName>
    <LocalizedNames>
      <LocalizedName description="$(Escape $identity.displayName)" languagecode="1033" />
    </LocalizedNames>
    <Descriptions>
      <Description description="$(Escape $identity.description)" languagecode="1033" />
    </Descriptions>
    <Version>$version</Version>
    <Managed>0</Managed>
$publisher
    <RootComponents>
      <RootComponent type="91" id="{$($identity.assemblyId)}" schemaName="$(Escape $assembly.FullName)" behavior="0" />
    </RootComponents>
    <MissingDependencies />
  </SolutionManifest>
</ImportExportXml>
"@ | Set-Content (Join-Path $folder 'Other\Solution.xml') -Encoding utf8

    @'
<?xml version="1.0" encoding="utf-8"?>
<ImportExportXml xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" OrganizationVersion="9.2.26094.133" OrganizationSchemaType="Standard" CRMServerServiceabilityVersion="9.2.26094.00133">
  <Entities />
  <Roles />
  <Workflows />
  <FieldSecurityProfiles />
  <Templates />
  <EntityMaps />
  <EntityRelationships />
  <OrganizationSettings />
  <optionsets />
  <CustomControls />
  <SolutionPluginAssemblies />
  <EntityDataProviders />
  <Languages>
    <Language>1033</Language>
  </Languages>
</ImportExportXml>
'@ | Set-Content (Join-Path $folder 'Other\Customizations.xml') -Encoding utf8

    # ---- pack unmanaged, then managed from a copy marked as managed
    $zip = Join-Path $OutputFolder "$($identity.uniqueName)_$($version.Replace('.', '_')).zip"
    $managedZip = Join-Path $OutputFolder "$($identity.uniqueName)_$($version.Replace('.', '_'))_managed.zip"
    $managedFolder = "$folder-managed"
    if (Test-Path $managedFolder) { Remove-Item $managedFolder -Recurse -Force }
    Copy-Item $folder $managedFolder -Recurse
    $managedManifest = Join-Path $managedFolder 'Other\Solution.xml'
    (Get-Content $managedManifest -Raw).Replace('<Managed>0</Managed>', '<Managed>1</Managed>') | Set-Content $managedManifest -Encoding utf8

    foreach ($package in @(@{ Zip = $zip; Folder = $folder; Type = 'Unmanaged' }, @{ Zip = $managedZip; Folder = $managedFolder; Type = 'Managed' })) {
        if (Test-Path $package.Zip) { Remove-Item $package.Zip }

        $output = & pac solution pack --zipfile $package.Zip --folder $package.Folder --packagetype $package.Type 2>&1

        # pac can report an error and still exit with 0, so check for the file too
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $package.Zip) -or ($output -match '^Error')) {
            $output | Out-Host
            throw "pac solution pack ($($package.Type)) failed for $($identity.uniqueName)."
        }

        Write-Host "  packed $($package.Type.ToLowerInvariant())"
        $built += $package.Zip
    }
}

Write-Host ''
Write-Host 'Solution files:' -ForegroundColor Green
$built | ForEach-Object { Write-Host "  $_" }
