<#
.SYNOPSIS
  Copies the entire repo (from its parent folder) excluding heavy/regenerable folders — suitable for archival or copying to a USB/server for rebuild.

.DESCRIPTION
  Excludes: node_modules, .angular, dist, bin, obj, .vs, publish, artifacts, and common caches.
  Includes: all source, SQL scripts, configs, solution files, package.json, etc. (everything needed to rebuild after npm ci + dotnet restore).

  Default output: sibling of repo folder, e.g. D:\OnlineHallBooking-source-backup-20260409-143022
  Use -Zip to also create a .zip next to the folder (can be slow/large if .git is included).

.PARAMETER RepoRoot
  Path to repository root (folder containing frontend, backend, shared). Default: parent of this script's directory.

.PARAMETER Destination
  Full path to the backup folder. If omitted, uses ..\OnlineHallBooking-source-backup-<timestamp> relative to repo root.

.PARAMETER ExcludeGit
  If set, omits the .git directory (smaller backup; you lose full git history in the copy).

.PARAMETER Zip
  If set, compresses the backup folder to <Destination>.zip after robocopy completes.

.EXAMPLE
  pwsh -File scripts/backup-project-source.ps1
  pwsh -File scripts/backup-project-source.ps1 -ExcludeGit -Zip
#>
[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [string] $Destination = "",
  [switch] $ExcludeGit,
  [switch] $Zip
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if (-not (Test-Path (Join-Path $RepoRoot "backend\OnlineBookingSystem.sln"))) {
  throw "RepoRoot does not look like this project (missing backend\OnlineBookingSystem.sln): $RepoRoot"
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (-not $Destination) {
  $parent = Split-Path $RepoRoot -Parent
  $Destination = Join-Path $parent "OnlineHallBooking-source-backup-$stamp"
}

if (Test-Path $Destination) {
  throw "Destination already exists: $Destination"
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null

# /E copy subdirs including empty; /XD exclude dir names anywhere in tree; /NFL /NDL /NJH /NJS quiet summary
$xd = @(
  "node_modules",
  ".angular",
  "dist",
  "bin",
  "obj",
  ".vs",
  "publish",
  "artifacts",
  "coverage",
  ".turbo",
  ".parcel-cache"
)
if ($ExcludeGit) {
  $xd += ".git"
}

Write-Host "Source:      $RepoRoot"
Write-Host "Destination: $Destination"
Write-Host "Exclusions:  $($xd -join ', ')"

# /XD excludes directory names anywhere under the tree (repeatable; one block is fine)
# Exclude local-only secrets file if present (use appsettings.Development.json.example in repo)
$rcArgs = @(
  $RepoRoot, $Destination, "/E", "/COPY:DAT", "/R:1", "/W:1",
  "/XF", "*.db", "*.db-shm", "*.db-wal", "appsettings.Development.json",
  "/XD"
) + $xd
& robocopy.exe @rcArgs | Out-Host
$rc = $LASTEXITCODE
# Robocopy: 0–7 = success (0 nothing, 1 files, 2 dirs, etc.); 8+ = failure
if ($rc -ge 8) {
  throw "robocopy failed with exit code $rc"
}

Write-Host "robocopy finished (exit $rc; 0–7 is normal). Backup folder ready."

if ($Zip) {
  $zipPath = "$Destination.zip"
  if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
  Write-Host "Creating zip: $zipPath"
  Compress-Archive -Path $Destination -DestinationPath $zipPath -CompressionLevel Optimal
  Write-Host "Zip created."
}

Write-Host ""
Write-Host "NEXT: To restore on another machine, copy the folder (or unzip), then:"
Write-Host "  cd frontend && npm ci && npm run start (or build)"
Write-Host "  dotnet restore backend/OnlineBookingSystem.sln"
