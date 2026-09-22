[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repositoryRoot
try {
    dotnet restore .\OneBoardInlineTranslate.sln
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    dotnet format .\OneBoardInlineTranslate.sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet format verification failed.' }

    dotnet build .\OneBoardInlineTranslate.sln -c Release -p:Platform=x64 --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Release x64 build failed.' }

    dotnet run --project .\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj -c Release -p:Platform=x64 --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
}
finally {
    Pop-Location
}
