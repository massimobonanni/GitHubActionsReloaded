[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateSet(0, 1, 2, 3)]
    [int]$Stage,
    [switch]$IncludeAgentic
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $root '.github\workflows'
$destination = Join-Path $directory 'excuseops.yml'

if ($Stage -eq 0) {
    $owned = @($destination)
    if ($IncludeAgentic) {
        $owned += Join-Path $directory 'repo-digest.md'
        $owned += Join-Path $directory 'repo-digest.lock.yml'
    }
    foreach ($path in $owned) {
        if ((Test-Path -LiteralPath $path) -and $PSCmdlet.ShouldProcess($path, 'Remove explicitly demo-owned workflow')) {
            Remove-Item -LiteralPath $path -Force
        }
    }
} else {
    $snapshot = @{ 1 = '01-ci.yml'; 2 = '02-test.yml'; 3 = '03-production.yml' }[$Stage]
    $source = Join-Path $root "workflows\$snapshot"
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing snapshot: $source" }
    if ($PSCmdlet.ShouldProcess($destination, "Activate $snapshot")) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -Force
    }
}
Write-Host 'Only explicitly demo-owned workflow paths are managed. No Git commands or remote actions were run.'
