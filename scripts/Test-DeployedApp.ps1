[CmdletBinding()]
param([switch]$AllowLocalHttp)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:DEMO_API_KEY)) { throw 'DEMO_API_KEY is required.' }
$uri = $null
if (-not [uri]::TryCreate($env:APP_URL, [UriKind]::Absolute, [ref]$uri)) { throw 'APP_URL is required and must be absolute.' }
if ($uri.UserInfo -or $uri.Query -or $uri.Fragment -or $uri.AbsolutePath -ne '/') { throw 'APP_URL must be an origin URL without credentials, path, query, or fragment.' }
if ($uri.Scheme -ne 'https' -and -not ($AllowLocalHttp -and $uri.Scheme -eq 'http' -and $uri.IsLoopback)) {
    throw 'HTTPS is required, except explicit local loopback validation.'
}

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(20)
try {
    $ready = $false
    for ($attempt = 1; $attempt -le 12; $attempt++) {
        $response = $null
        try {
            $response = $client.GetAsync("$($uri.AbsoluteUri.TrimEnd('/'))/health").GetAwaiter().GetResult()
            if ($response.IsSuccessStatusCode) {
                $health = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
                if ($health.status -ne 'healthy' -or $health.app -ne 'ExcuseOps') { throw 'Unexpected health response.' }
                if ($env:EXPECTED_STAGE -and $health.stage -ne $env:EXPECTED_STAGE) { throw 'Health response came from the wrong environment.' }
                $ready = $true
            }
        } catch [System.Net.Http.HttpRequestException] {
            Write-Host "Startup connection not ready (attempt $attempt/12)."
        } catch [System.Threading.Tasks.TaskCanceledException] {
            Write-Host "Startup request timed out (attempt $attempt/12)."
        } finally {
            if ($response) { $response.Dispose() }
        }
        if ($ready) { break }
        if ($attempt -lt 12) { Start-Sleep -Seconds 10 }
    }
    if (-not $ready) { throw 'Health check failed after bounded startup retries.' }

    $public = $client.GetAsync("$($uri.AbsoluteUri.TrimEnd('/'))/api/excuses?category=cloud&variant=0").GetAwaiter().GetResult()
    try {
        if (-not $public.IsSuccessStatusCode) { throw 'Public excuse endpoint failed.' }
    } finally { $public.Dispose() }

    $anonymous = $client.GetAsync("$($uri.AbsoluteUri.TrimEnd('/'))/api/vip-excuse").GetAwaiter().GetResult()
    try {
        if ([int]$anonymous.StatusCode -ne 401) { throw 'VIP endpoint did not reject the anonymous request.' }
    } finally { $anonymous.Dispose() }

    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, "$($uri.AbsoluteUri.TrimEnd('/'))/api/vip-excuse")
    try {
        $request.Headers.Add('X-Demo-Key', $env:DEMO_API_KEY)
        $vip = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            if (-not $vip.IsSuccessStatusCode) { throw "VIP smoke test failed (HTTP $([int]$vip.StatusCode)); check the environment secret and App Service configuration." }
            $body = $vip.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
            if ($body.category -ne 'vip') { throw 'Unexpected VIP response.' }
        } finally { $vip.Dispose() }
    } finally { $request.Dispose() }
    Write-Host 'PASS: health, public excuse, anonymous rejection, authenticated VIP. Key was not logged.'
} finally {
    $client.Dispose()
}
