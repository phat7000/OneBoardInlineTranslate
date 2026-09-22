[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$phase0Root = Split-Path -Parent $PSScriptRoot

dotnet build "$phase0Root\OneBoardInlineTranslate.sln" -c Release -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    throw "Phase 0 build failed with exit code $LASTEXITCODE."
}

dotnet run `
    --project "$phase0Root\tests\OneBoardInlineTranslate.SmokeTests\OneBoardInlineTranslate.SmokeTests.csproj" `
    -c Release `
    -p:Platform=x64 `
    --no-build
if ($LASTEXITCODE -ne 0) {
    throw "Phase 0 smoke checks failed with exit code $LASTEXITCODE."
}
