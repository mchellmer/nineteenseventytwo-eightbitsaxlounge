<#: 
Load .env (or .env.template) into the current PowerShell session
and run the UI using the repo venv Python.

Usage:
  .\ui\scripts\run_with_env.ps1

This sets environment variables only for the running session and child
processes started by this script.
#>

try {
    $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $UIRoot = Resolve-Path (Join-Path $ScriptRoot "..")
    $EnvFile = Join-Path $UIRoot ".env"
    if (-not (Test-Path $EnvFile)) {
        $EnvFile = Join-Path $UIRoot ".env.template"
    }

    if (-not (Test-Path $EnvFile)) {
        Write-Error "No .env or .env.template found in $UIRoot"
        exit 1
    }

    Write-Host "Loading environment from: $EnvFile"
    Get-Content $EnvFile | ForEach-Object {
        $line = $_.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { return }
        $parts = $line -split '=', 2
        if ($parts.Count -lt 2) { return }
        $name = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"').Trim("'")
        # Set in current session using Set-Item for dynamic names
        Set-Item -Path ("Env:$name") -Value $value
        Write-Host "Set $name"
    }

    $Python = Join-Path $UIRoot ".venv\Scripts\python.exe"
    $Main = Join-Path $UIRoot "src\main.py"

    if (-not (Test-Path $Python)) {
        Write-Error "Python executable not found at: $Python. Activate venv or create .venv first."
        exit 2
    }

    Write-Host "Running: $Python $Main"
    & $Python $Main
}
catch {
    Write-Error "Error while loading env or running main: $_"
    exit 3
}
