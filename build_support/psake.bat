@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module '..\source\packages\psake.4.0.1.0\tools\psake.psm1'; invoke-psake -t default;}"
pause