[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'

foreach ($name in 'AZURE_CLIENT_ID', 'AZURE_TENANT_ID', 'AZURE_SUBSCRIPTION_ID', 'AZURE_WEBAPP_NAME', 'AZURE_RESOURCE_GROUP', 'APP_URL', 'DEMO_API_KEY') {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Missing environment configuration: $name. Configure the GitHub environment, not only repository defaults."
    }
}
foreach ($name in 'AZURE_CLIENT_ID', 'AZURE_TENANT_ID', 'AZURE_SUBSCRIPTION_ID') {
    $parsed = [guid]::Empty
    if (-not [guid]::TryParse([Environment]::GetEnvironmentVariable($name), [ref]$parsed)) {
        throw "$name must be a GUID."
    }
}
$uri = $null
if (-not [uri]::TryCreate($env:APP_URL, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https' -or $uri.UserInfo) {
    throw 'APP_URL must be an absolute HTTPS URL without credentials.'
}
if ($uri.Host -ne "$($env:AZURE_WEBAPP_NAME).azurewebsites.net") {
    throw 'APP_URL must match this environment Azure Web App; do not send the demo key to an arbitrary host.'
}
Write-Host 'Required environment configuration is present. Secret values are not printed.'
