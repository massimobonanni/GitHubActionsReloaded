[CmdletBinding()]
param([string]$Dotnet = 'dotnet')

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root 'out\publish'
$zip = Join-Path $root 'out\excuseops.zip'
if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
New-Item -ItemType Directory -Path $publish -Force | Out-Null
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }

Push-Location $root
try {
    & $Dotnet publish 'src/ExcuseOps.Web/ExcuseOps.Web.csproj' --configuration Release --no-restore --no-self-contained -p:UseAppHost=false --output $publish --nologo
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $zip)
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
    try {
        if ('ExcuseOps.Web.dll' -notin $archive.Entries.FullName) {
            throw 'ZIP must contain application files at its root, not an enclosing publish folder.'
        }
    } finally { $archive.Dispose() }
    Write-Host "Deployment ZIP: $zip"
    Write-Host "SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
} finally { Pop-Location }
