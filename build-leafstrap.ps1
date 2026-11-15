param(
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$localDotnet = Join-Path $projectRoot '.dotnet-sdk\dotnet.exe'
$projectFile = Join-Path $projectRoot 'Bloxstrap\Bloxstrap.csproj'

if (Test-Path -LiteralPath $localDotnet) {
    $dotnet = $localDotnet
}
else {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw 'The .NET 10 SDK is required, but dotnet was not found.'
    }

    $dotnet = $dotnetCommand.Source
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.nuget-packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

if (-not $SkipRestore) {
    & $dotnet restore $projectFile --nologo --runtime win-x64
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }
}

& $dotnet build $projectFile -c Release --no-restore --nologo -r win-x64
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

& $dotnet publish $projectFile -c Release -p:PublishProfile=Publish-x64 --no-restore --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedExecutable = Join-Path $projectRoot 'Bloxstrap\bin\Release\net10.0-windows\publish\win-x64\Leafstrap.exe'
Write-Host "Leafstrap published to: $publishedExecutable"
