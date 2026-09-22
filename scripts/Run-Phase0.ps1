[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$phase0Root = Split-Path -Parent $PSScriptRoot

dotnet run `
    --project "$phase0Root\src\OneBoardInlineTranslate\OneBoardInlineTranslate.csproj" `
    -c Release `
    -p:Platform=x64
