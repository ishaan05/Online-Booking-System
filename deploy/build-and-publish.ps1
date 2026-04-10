# Build production Angular app, copy into API wwwroot, publish .NET (Release).
# Run from repo root:  powershell -File deploy/build-and-publish.ps1
# For a clean package under artifacts\ (recommended before copying to a server), use: scripts/create-windows-server-package.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path "$root/frontend/package.json")) {
  throw "Run this script from the repo (expected $root/frontend/package.json)."
}

Write-Host "Repo root: $root"

Push-Location "$root/frontend"
if (-not (Test-Path "node_modules")) {
  npm ci
}
npm run build -- --configuration production
Pop-Location

$dist = Join-Path $root "frontend/dist/online-booking-system"
$www = Join-Path $root "backend/OnlineBookingSystem.Api/wwwroot"
if (-not (Test-Path $dist)) {
  throw "Angular dist not found: $dist (check angular.json outputPath)"
}
New-Item -ItemType Directory -Force -Path $www | Out-Null
Get-ChildItem -Path $www -Exclude ".gitkeep" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path "$dist/*" -Destination $www -Recurse -Force

Push-Location "$root/backend/OnlineBookingSystem.Api"
$out = Join-Path $root "backend/OnlineBookingSystem.Api/publish"
dotnet publish -c Release -o $out
Pop-Location

Write-Host "Published to: $out"
Write-Host "Set ASPNETCORE_ENVIRONMENT=Production, ConnectionStrings__DefaultConnection, Jwt__Key (32+ chars), Provisioning__MintKey before hosting."
