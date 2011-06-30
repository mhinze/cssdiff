@echo off
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& { Import-Module '.\source\packages\psake.4.0.1.0\tools\psake.psm1'; invoke-psake .\build_support\default.ps1 -t %1 ; if ($lastexitcode -ne 0) {write-host "ERROR: $lastexitcode" -fore RED; exit $lastexitcode} }"
pause
