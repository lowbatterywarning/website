param(
    [string]$Dotnet = 'dotnet',
    [string]$Configuration = 'Release',
    [ValidateSet('Production', 'Development')]
    [string]$Environment = 'Production',
    [switch]$DisableProxy
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dll = Join-Path $repo "Zamfara.Web/bin/$Configuration/net10.0/Zamfara.Web.dll"
if (!(Test-Path $dll)) { throw 'Build the solution before running this check.' }
$temp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$start = [Diagnostics.ProcessStartInfo]::new($Dotnet)
$start.ArgumentList.Add($dll)
$start.WorkingDirectory = Join-Path $repo 'Zamfara.Web'
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.Environment['Logging__LogLevel__Default'] = 'Warning'
$start.Environment['Logging__EventLog__LogLevel__Default'] = 'None'
$start.Environment['LOCALAPPDATA'] = $temp
$start.Environment['APPDATA'] = $temp
$start.Environment['ASPNETCORE_ENVIRONMENT'] = $Environment
$start.Environment['ASPNETCORE_URLS'] = "http://127.0.0.1:$port"
$start.Environment['ASPNETCORE_HTTPS_PORT'] = '443'
# Leave the old setting on deliberately: the app must suppress its automatic middleware.
$start.Environment['ASPNETCORE_FORWARDEDHEADERS_ENABLED'] = 'true'
$start.Environment['ZAMFARA_FORWARDEDHEADERS_ENABLED'] = (!$DisableProxy).ToString()
$start.Environment['ZAMFARA_DB_PATH'] = Join-Path $temp 'test.db'
$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(5)
$count = 0

function Check-Route($hostName, $path, $status, $location = $null, $contains = $null, $method = 'GET', $forwarded = $true, [switch]$capture) {
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($method), "http://127.0.0.1:$port$path")
    $request.Headers.Host = $hostName
    if ($forwarded) { $request.Headers.Add('X-Forwarded-Proto', 'https') }
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    try {
        if ([int]$response.StatusCode -ne $status) { throw "$method $hostName$path returned $([int]$response.StatusCode), expected $status" }
        if ($location -and $response.Headers.Location.OriginalString -ne $location) { throw "Unexpected redirect for ${hostName}${path}: $($response.Headers.Location.OriginalString), expected $location" }
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($contains -and !$body.Contains($contains)) { throw "Expected content missing on $hostName$path" }
        $script:count++
        if ($capture) { return $body }
    } finally { $response.Dispose(); $request.Dispose() }
}

$process = $null
try {
    $process = [Diagnostics.Process]::Start($start)
    $output = $process.StandardOutput.ReadToEndAsync()
    $errors = $process.StandardError.ReadToEndAsync()
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($process.HasExited) { throw "App exited before becoming healthy: $($output.GetAwaiter().GetResult()) $($errors.GetAwaiter().GetResult())" }
        try {
            $probe = $client.GetAsync("http://127.0.0.1:$port/healthz").GetAwaiter().GetResult()
            $ready = [int]$probe.StatusCode -eq 200
            $probe.Dispose()
        } catch { }
        if ($ready) { break }
        Start-Sleep -Milliseconds 500
    }
    if (!$ready) { throw 'App did not become healthy.' }
    if ($DisableProxy) {
        Check-Route 'gsss.zamfara.org' '/about' 307 'https://gsss.zamfara.org/about'
        Write-Host 'Legacy automatic proxy flag suppression passed.'
        return
    }
    $pages = @('/about','/academics','/admissions','/news','/staff','/calendar','/gallery','/faq')
    foreach ($hostName in @('zamfara.org','www.zamfara.org','unknown.zamfara.org')) {
        Check-Route $hostName '/' 200 -contains 'School Directory'
        foreach ($page in $pages) {
            Check-Route $hostName $page 302 '/'
            Check-Route $hostName "$page/" 302 '/'
            Check-Route $hostName $page 302 '/' -method 'HEAD'
        }
        Check-Route $hostName '/Home/Error' 200 -contains 'Something went wrong'
        Check-Route $hostName '/missing' 404
        Check-Route $hostName '/about.html' 301 '/about'
    }
    foreach ($hostName in @('gsss.zamfara.org','demo-one.zamfara.org','demo-two.zamfara.org','localhost')) {
        foreach ($page in (@('/') + $pages)) { Check-Route $hostName $page 200 }
        Check-Route $hostName '/ABOUT.HTML/' 301 '/about'
        Check-Route $hostName '/school-two/admissions' 301 '/admissions'
        Check-Route $hostName '/contact.html' 301 '/'
    }
    foreach ($hostName in @('zamfara.org','www.zamfara.org','gsss.zamfara.org','demo-one.zamfara.org','demo-two.zamfara.org')) {
        $sitemapBody = Check-Route $hostName '/sitemap.xml' 200 -capture
        [xml]$sitemap = $sitemapBody
        $locations = @($sitemap.urlset.url | ForEach-Object { $_.loc })
        $portal = $hostName -in @('zamfara.org','www.zamfara.org')
        $origin = if ($portal) { 'https://zamfara.org' } else { "https://$hostName" }
        $expectedPaths = if ($portal) { @('/') } else { @('/') + $pages }
        $expected = @($expectedPaths | ForEach-Object { "$origin$_" })
        if (Compare-Object $expected $locations) { throw "Incorrect sitemap URLs for $hostName" }
        Check-Route $hostName '/robots.txt' 200 -contains "Sitemap: $origin/sitemap.xml"
    }
    if ($Environment -eq 'Production') {
        Check-Route 'zamfara.org' '/about?school=gsss' 302 'https://gsss.zamfara.org/about'
        Check-Route 'zamfara.org' '/news?school=demo-one&search=a%20b&tag=1&tag=2' 302 'https://demo-one.zamfara.org/news?search=a%20b&tag=1&tag=2'
        Check-Route 'gsss.zamfara.org' '/about?school=demo-two' 302 'https://demo-two.zamfara.org/about'
        Check-Route 'zamfara.org' '/about?school=invalid' 302 '/'
        Check-Route 'gsss.zamfara.org' '/about?school=invalid' 200 -contains 'Government Science Secondary School'
        Check-Route 'gsss.zamfara.org' '/about' 307 'https://gsss.zamfara.org/about' -forwarded $false
    } else {
        $selectedHomeHtml = Check-Route 'localhost' '/?school=demo-one' 200 -contains 'Demo Science Secondary School' -capture
        if ($selectedHomeHtml -notmatch 'href="/about\?school=demo-one"') { throw 'Development navigation dropped the selected school.' }
        Check-Route 'localhost' '/about?school=demo-one' 200 -contains 'Demo Science Secondary School'
        Check-Route 'zamfara.org' '/about?school=gsss' 200 -contains 'Government Science Secondary School'
    }
    Check-Route 'zamfara.org' '/about' 405 -method 'POST'
    Write-Host "Passed $count route checks in $Environment with a temporary SQLite database."

} finally {
    if ($process -and !$process.HasExited) { $process.Kill($true); $process.WaitForExit() }
    $client.Dispose()
    $handler.Dispose()
    # Delete only the unique temporary directory this script created.
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTemp = [IO.Path]::GetFullPath($temp)
    if (!$resolvedTemp.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Temporary cleanup path escaped the system temp directory.' }
    Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
}
