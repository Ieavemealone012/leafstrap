$ErrorActionPreference = "Stop"

$installerRoot = $PSScriptRoot
$windowsRoot = Split-Path $installerRoot -Parent
$workspaceRoot = Split-Path $windowsRoot -Parent
$dotnetTool = Join-Path $workspaceRoot ".dotnet-sdk\dotnet.exe"
$project = Join-Path $windowsRoot "Bloxstrap\Bloxstrap.csproj"
$publishDirectory = Join-Path $windowsRoot "Bloxstrap\bin\Release\net10.0-windows\publish\win-x64"

if (-not (Test-Path -LiteralPath $dotnetTool)) {
    $dotnetTool = "dotnet"
}

& (Join-Path $installerRoot "generate-installer-assets.ps1")
if (-not $?) { exit 1 }

& $dotnetTool publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $publishDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnetTool build (Join-Path $installerRoot "LeafstrapInstaller.wixproj") -c Release -p:AppVersion=1.0.7 -p:PublishDir=$publishDirectory
exit $LASTEXITCODE
