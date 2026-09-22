[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactsDirectory = Join-Path $repositoryRoot 'artifacts'
$publishDirectory = Join-Path $artifactsDirectory 'publish'
$zipPath = Join-Path $artifactsDirectory 'OneBoardInlineTranslate-1.0.0-win-x64.zip'

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'Build-Release.ps1')
}

if (Test-Path -LiteralPath $publishDirectory) {
    $resolvedPublish = (Resolve-Path -LiteralPath $publishDirectory).Path
    if (-not $resolvedPublish.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove an unexpected publish directory: $resolvedPublish"
    }
    Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
Push-Location $repositoryRoot
try {
    dotnet publish .\src\OneBoardInlineTranslate\OneBoardInlineTranslate.csproj `
        -c Release -r win-x64 --self-contained true `
        -p:Platform=x64 -p:PublishReadyToRun=true `
        -o $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed.' }
}
finally {
    Pop-Location
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal

if (-not $SkipInstaller) {
    $compilerCandidates = @(
        (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    ) | Where-Object { $_ }
    $compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $compiler) {
        throw 'Inno Setup 6 is required to build the installer. Install it or use -SkipInstaller.'
    }

    & $compiler "/DPublishDir=$publishDirectory" "/DOutputDir=$artifactsDirectory" (Join-Path $repositoryRoot 'packaging\OneBoardInlineTranslate.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
}

Write-Host "Portable package: $zipPath"
if (-not $SkipInstaller) {
    Write-Host "Installer: $(Join-Path $artifactsDirectory 'OneBoardInlineTranslate-Setup-1.0.0-win-x64.exe')"
}
