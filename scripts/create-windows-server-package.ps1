<#
.SYNOPSIS
  Builds production Angular + publishes the API to artifacts\<timestamp>\Api without leaving SPA files in your repo wwwroot.

.DESCRIPTION
  1) npm run build (production) in frontend
  2) Copies build output into a temporary copy of the API project (so your working tree wwwroot is not used), OR briefly stages wwwroot then restores it
  This script STAGES into backend\OnlineBookingSystem.Api\wwwroot then REMOVES all files except .gitkeep after publish - same as deploy/build-and-publish.ps1 cleanup so localhost (ng serve + API) keeps working.

  Output: artifacts\windows-server-yyyyMMdd-HHmmss\Api\  (IIS-ready publish folder)

.PARAMETER RepoRoot
  Repository root. Default: parent of scripts folder.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts\create-windows-server-package.ps1
#>
[CmdletBinding()]
param(
  [string] $RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

$frontend = Join-Path $RepoRoot "frontend"
$apiProjDir = Join-Path $RepoRoot "backend\OnlineBookingSystem.Api"
$apiCsproj = Join-Path $apiProjDir "OnlineBookingSystem.Api.csproj"
$dist = Join-Path $RepoRoot "frontend\dist\online-booking-system"
$www = Join-Path $apiProjDir "wwwroot"
$artifactsRoot = Join-Path $RepoRoot "artifacts"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$out = Join-Path $artifactsRoot "windows-server-$stamp\Api"

if (-not (Test-Path $apiCsproj)) { throw "API project not found: $apiCsproj" }
if (-not (Test-Path (Join-Path $frontend "package.json"))) { throw "frontend/package.json missing" }

Write-Host "Repo root: $RepoRoot"
Write-Host "Publish output: $out"

Push-Location $frontend
try {
  if (-not (Test-Path "node_modules")) {
    Write-Host "Running npm ci..."
    npm ci
  }
  Write-Host "Building Angular (production)..."
  npm run build -- --configuration production
}
finally {
  Pop-Location
}

if (-not (Test-Path $dist)) {
  throw "Angular output not found: $dist (check angular.json outputPath)"
}

New-Item -ItemType Directory -Force -Path $www | Out-Null
Get-ChildItem -Path $www -Force | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $dist "*") -Destination $www -Recurse -Force

New-Item -ItemType Directory -Force -Path $out | Out-Null
Push-Location $apiProjDir
try {
  Write-Host "dotnet publish Release -> $out"
  dotnet publish $apiCsproj -c Release -o $out --no-self-contained
}
finally {
  Pop-Location
}

# Restore wwwroot for local dev (SPA served by ng serve; empty wwwroot avoids stale production files)
Get-ChildItem -Path $www -Force | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
if (-not (Test-Path (Join-Path $www ".gitkeep"))) {
  New-Item -ItemType File -Path (Join-Path $www ".gitkeep") -Force | Out-Null
}

Write-Host ""
Write-Host "DONE. IIS-ready files are at:"
Write-Host "  $out"
Write-Host ""
Write-Host "Read docs/WINDOWS-SERVER-DEPLOYMENT.md for IIS, SQL Server, and environment variables."
Write-Host 'On first production run, Jwt:Key may be auto-created as App_Data\jwt-signing.key in the deployed folder - grant the app pool Modify on that folder and back up that file with the server.'
