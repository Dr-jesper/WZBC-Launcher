[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')][string]$Architecture = 'x64',
    [ValidateSet('Beta', 'Release')][string]$Configuration = 'Beta',
    [string]$Dotnet = 'dotnet',
    [string]$OutputRoot = 'artifacts/wzbc',
    [string]$EnvFile = '.env',
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path (Join-Path $repo $OutputRoot) "win-$($Architecture.ToLowerInvariant())"
if (Test-Path $output) {
    throw "Output already exists: $output. Move it aside before publishing again."
}
# Only the public Microsoft app identifier is supported here. Never evaluate .env as code.
$clientId = $env:PCL_MS_CLIENT_ID
$envPath = Join-Path $repo $EnvFile
if (Test-Path -LiteralPath $envPath) {
    foreach ($line in Get-Content -LiteralPath $envPath) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
        if ($line -notmatch '^\s*PCL_MS_CLIENT_ID\s*=\s*([^\s]*)\s*$') {
            throw '.env supports only an unquoted PCL_MS_CLIENT_ID=value assignment.'
        }
        if (-not $clientId) { $clientId = $Matches[1] }
    }
}
if ($clientId -and -not [guid]::TryParse($clientId, [ref]([guid]::Empty))) {
    throw 'PCL_MS_CLIENT_ID must be an application GUID.'
}
$savedPclEnvironment = @{}
Get-ChildItem Env:PCL_* | ForEach-Object { $savedPclEnvironment[$_.Name] = $_.Value }
Push-Location $repo
try {
    # The upstream generator embeds every PCL_* variable. Isolate its inputs.
    Get-ChildItem Env:PCL_* | ForEach-Object { [Environment]::SetEnvironmentVariable($_.Name, $null, 'Process') }
    $env:PCL_WRITE_SECRET = '1'
    $env:PCL_MS_CLIENT_ID = $clientId
    $env:PCL_GITHUB_SHA = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve source commit.' }
    # Force regeneration when .env changes; compiler servers can retain stale environment values.
    & $Dotnet clean 'Plain Craft Launcher 2/Plain Craft Launcher 2.csproj' -c $Configuration "-p:Platform=$Architecture" -v quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed: $LASTEXITCODE" }
    & $Dotnet publish 'Plain Craft Launcher 2/Plain Craft Launcher 2.csproj' -c $Configuration "-p:Platform=$Architecture" -p:UseSharedCompilation=false --self-contained true -o $output
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }
    $name = "WZBC-Launcher-win-$($Architecture.ToLowerInvariant())"
    Move-Item -LiteralPath (Join-Path $output 'Plain Craft Launcher 2.exe') -Destination (Join-Path $output "$name.exe")
    Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $output
    Copy-Item -LiteralPath (Join-Path $repo 'Plain Craft Launcher 2/LICENCE') -Destination $output
    Copy-Item -LiteralPath (Join-Path $repo 'docs/wzbc/BUILD.md') -Destination (Join-Path $output 'BUILD.md')
    Copy-Item -LiteralPath (Join-Path $repo 'docs/wzbc/PENDING.md') -Destination (Join-Path $output 'PENDING.md')
    foreach ($document in @('PRIVACY.md', 'SECURITY.md', 'LICENSING.md')) {
        Copy-Item -LiteralPath (Join-Path $repo $document) -Destination $output
    }
    $commit = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve source commit.' }
    $sdk = & $Dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve SDK version.' }
    $sourceState = if (git status --porcelain) { 'modified working tree (includes uncommitted changes)' } else { 'clean' }
    $appConfigured = -not [string]::IsNullOrWhiteSpace($clientId)
    $metadata = Get-Content -LiteralPath (Join-Path $repo 'Plain Craft Launcher 2/metadata.json') -Raw | ConvertFrom-Json
    $releaseSource = Get-Content -LiteralPath (Join-Path $repo 'PCL.Core/Conduit/ClubRelease.cs') -Raw
    $codename = [regex]::Match($releaseSource, 'CurrentCodename = "([^"]+)"').Groups[1].Value
    $version = $metadata.version.base + $(if ($metadata.version.suffix) { '-' + $metadata.version.suffix })
    "Version: $version`nCodename (SHOU-PCL baseline): $codename`nSource commit: $commit`nSource state: $sourceState`nSDK: $sdk`nConfiguration: $Configuration`nRuntime: win-$($Architecture.ToLowerInvariant())`nSelf-contained: true`nMicrosoft public Client ID configured: $appConfigured`nOther service credentials: not embedded" | Set-Content (Join-Path $output 'BUILD-INFO.txt') -Encoding utf8
    if ($BuildOnly) {
        Write-Host "Build verified; no distribution archive created: $output"
        return
    }
    $archive = Join-Path (Split-Path $output) "$name.zip"
    # Package only distribution files, never runtime-created profiles, caches or logs.
    $packageNames = @("$name.exe", 'LICENSE', 'LICENCE', 'BUILD.md', 'PENDING.md', 'BUILD-INFO.txt',
        'PRIVACY.md', 'SECURITY.md', 'LICENSING.md')
    $actualNames = @(Get-ChildItem -LiteralPath $output | ForEach-Object Name)
    if (@(Compare-Object $packageNames $actualNames).Count -ne 0) {
        throw 'Unexpected publish output; inspect it before packaging.'
    }
    Compress-Archive -LiteralPath @($packageNames | ForEach-Object { Join-Path $output $_ }) -DestinationPath $archive
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $name.zip" | Set-Content "$archive.sha256" -Encoding ascii
    Write-Host "Published: $archive"
} finally {
    Get-ChildItem Env:PCL_* | ForEach-Object { [Environment]::SetEnvironmentVariable($_.Name, $null, 'Process') }
    foreach ($name in $savedPclEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $savedPclEnvironment[$name], 'Process')
    }
    Pop-Location
}
