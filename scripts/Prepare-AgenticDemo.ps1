[CmdletBinding(SupportsShouldProcess)]
param([switch]$Compile)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'agentic\repo-digest.md'
$directory = Join-Path $root '.github\workflows'
$destination = Join-Path $directory 'repo-digest.md'

if ($PSCmdlet.ShouldProcess($destination, 'Copy the manual-only agentic Markdown source')) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}
if ($Compile -and $PSCmdlet.ShouldProcess($destination, 'Compile locally using gh aw; do not trigger a run')) {
    Push-Location $root
    try {
        & gh aw --version
        if ($LASTEXITCODE -ne 0) { throw 'GitHub Agentic Workflows is missing. Install the documented gh-aw extension first.' }
        & gh aw compile repo-digest --strict --no-check-update
        if ($LASTEXITCODE -ne 0) { throw 'Agentic compilation failed. Resolve diagnostics; no lock file should be invented manually.' }
        if (-not (Test-Path -LiteralPath (Join-Path $directory 'repo-digest.lock.yml'))) {
            throw 'Compiler did not emit the expected lock file.'
        }
    } finally { Pop-Location }
}
Write-Host 'No remote workflow was run. Review the generated lock file before committing it.'
