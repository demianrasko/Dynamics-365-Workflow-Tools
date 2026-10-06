<#
.SYNOPSIS
    Compares two runs of Invoke-UpgradeTestRun.ps1, e.g. before and after the upgrade.

.DESCRIPTION
    Lines up the two runs workflow by workflow: the system job's result, the log notes and the checks. Values that
    change on every run anyway are masked before comparing: ids (GUIDs), dates and times, and the workflow tools
    version. Writes comparison.md to the After run's folder.

    The test plan's "Differences that are expected" (testing\Upgrade test workflows.md) lists the fixes that should show
    up; anything else that differs is a regression to look at.

.PARAMETER Before
    The earlier run's folder (or its results.json).

.PARAMETER After
    The later run's folder (or its results.json).

.EXAMPLE
    .\tools\Compare-UpgradeTestRuns.ps1 -Before 'testing\results\2026-10-06 1930 before' -After 'testing\results\2026-10-08 1000 after'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Before,
    [Parameter(Mandatory)][string]$After
)

$ErrorActionPreference = 'Stop'

function Read-Run([string]$path) {
    if (Test-Path $path -PathType Container) {
        $path = Join-Path $path 'results.json'
    }

    return Get-Content $path -Raw -Encoding utf8 | ConvertFrom-Json
}

# masks what differs on every run
function Get-Masked([string]$text) {
    if (-not $text) {
        return ''
    }

    $text = $text -replace '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}', '<id>'
    $text = $text -replace '\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?', '<date>'
    $text = $text -replace '\d{1,2}/\d{1,2}/\d{4}( \d{1,2}:\d{2}(:\d{2})?( ?[AP]M)?)?', '<date>'
    $text = $text -replace '1\.0\.\d+\.\d+', '<version>'
    # Get App Record Url's URL has a new history key every time
    $text = $text -replace 'histKey=\d+', 'histKey=<n>'

    return $text.Trim()
}

# a workflow's results as labelled lines, ready to compare
function Get-Lines($workflow) {
    $lines = [Collections.Generic.List[string]]::new()

    if (-not $workflow) {
        return $lines
    }

    $lines.Add("Result: $($workflow.status)")

    if ($workflow.message) {
        $lines.Add("Message: $(Get-Masked ($workflow.message -replace '\s+', ' '))")
    }

    $n = 0

    foreach ($note in $workflow.notes) {
        $n++
        $lines.Add("Note $n subject: $(Get-Masked $note.subject)")

        if ($note.file) {
            $lines.Add("Note $n file: $($note.file)")
        }

        foreach ($line in ((Get-Masked ($note.text -replace '<br\s*/?>', "`n")) -split '\r?\n')) {
            $lines.Add("Note $n | $($line.TrimEnd())")
        }
    }

    if ($workflow.checks) {
        foreach ($check in $workflow.checks.PSObject.Properties) {
            $lines.Add("$($check.Name): $(Get-Masked $check.Value)")
        }
    }

    return $lines
}

$beforeRun = Read-Run $Before
$afterRun = Read-Run $After
$names = [Collections.Generic.List[string]]::new()

foreach ($workflow in @($beforeRun.workflows) + @($afterRun.workflows)) {
    if (-not $names.Contains($workflow.name)) {
        $names.Add($workflow.name)
    }
}

$report = [Text.StringBuilder]::new()
[void]$report.AppendLine("# Upgrade test comparison: $($beforeRun.label) → $($afterRun.label)")
[void]$report.AppendLine()
[void]$report.AppendLine("Before: workflow tools $($beforeRun.workflowTools), $($beforeRun.started). After: workflow tools $($afterRun.workflowTools), $($afterRun.started).")
[void]$report.AppendLine()
[void]$report.AppendLine('Ids, dates and the version are masked. Check each difference against "Differences that are expected" in testing\Upgrade test workflows.md.')
[void]$report.AppendLine()
[void]$report.AppendLine('| Workflow | Before | After | Same |')
[void]$report.AppendLine('| --- | --- | --- | --- |')
$details = [Text.StringBuilder]::new()
$differences = 0

foreach ($name in $names) {
    $old = @($beforeRun.workflows | Where-Object { $_.name -eq $name })[0]
    $new = @($afterRun.workflows | Where-Object { $_.name -eq $name })[0]
    $oldLines = Get-Lines $old
    $newLines = Get-Lines $new
    $diff = @(Compare-Object -ReferenceObject @($oldLines) -DifferenceObject @($newLines) -SyncWindow 1000 -CaseSensitive)
    $same = $diff.Count -eq 0
    $oldStatus = if ($old) { $old.status } else { '(not run)' }
    $newStatus = if ($new) { $new.status } else { '(not run)' }
    [void]$report.AppendLine("| $name | $oldStatus | $newStatus | $(if ($same) { 'yes' } else { '**no**' }) |")

    if (-not $same) {
        $differences++
        [void]$details.AppendLine()
        [void]$details.AppendLine("## $name")
        [void]$details.AppendLine()
        [void]$details.AppendLine('```diff')

        foreach ($line in $diff | Where-Object { $_.SideIndicator -eq '<=' }) {
            [void]$details.AppendLine("- $($line.InputObject)")
        }

        foreach ($line in $diff | Where-Object { $_.SideIndicator -eq '=>' }) {
            [void]$details.AppendLine("+ $($line.InputObject)")
        }

        [void]$details.AppendLine('```')
    }
}

[void]$report.Append($details.ToString())
$folder = if (Test-Path $After -PathType Container) { $After } else { Split-Path $After }
$output = Join-Path $folder 'comparison.md'
[IO.File]::WriteAllText($output, $report.ToString(), [Text.UTF8Encoding]::new($false))

Write-Host "$($names.Count - $differences) of $($names.Count) workflows the same. Comparison: $output" -ForegroundColor Cyan
