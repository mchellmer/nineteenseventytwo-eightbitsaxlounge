# backup-obs.ps1
# Copies OBS scenes and profiles into overlay/obs/ for source control.
# Run this after making changes in OBS to snapshot the current configuration.
#
# Usage:
#   .\backup-obs.ps1
#   .\backup-obs.ps1 -WhatIf   # Preview without copying

[CmdletBinding(SupportsShouldProcess)]
param()

$obsBase    = Join-Path $env:APPDATA "obs-studio\basic"
$repoBase   = Join-Path $PSScriptRoot "..\files\obs"

$targets = @(
    @{ src = Join-Path $obsBase "scenes";   dst = Join-Path $repoBase "scenes" },
    @{ src = Join-Path $obsBase "profiles"; dst = Join-Path $repoBase "profiles" }
)

foreach ($t in $targets) {
    if (-not (Test-Path $t.src)) {
        Write-Warning "Source not found, skipping: $($t.src)"
        continue
    }

    if ($PSCmdlet.ShouldProcess($t.dst, "Copy from $($t.src)")) {
        New-Item -ItemType Directory -Path $t.dst -Force | Out-Null
        Copy-Item -Path "$($t.src)\*" -Destination $t.dst -Recurse -Force
        Write-Host "Backed up: $($t.src) -> $($t.dst)"
    }
}

Write-Host "Done. Review changes with: git diff overlay/obs/"
