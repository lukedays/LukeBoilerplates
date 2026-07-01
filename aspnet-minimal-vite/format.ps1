# Formata todo o boilerplate: backend com CSharpier, frontend com Prettier.
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet tool restore
    dotnet csharpier format server
    npm --prefix client run format
}
finally {
    Pop-Location
}
