$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet workload install wasm-tools
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet publish Web/Web.csproj -c Release
if ($LASTEXITCODE) { exit $LASTEXITCODE }
npx wrangler deploy
exit $LASTEXITCODE
