$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
dotnet run --project .\src\DebugForgeStudio.Web\DebugForgeStudio.Web.csproj
