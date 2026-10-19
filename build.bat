@echo off
REM Compiles GameData\AutoStrutFixMod\AutoStrutFixMod.dll. Installing is up to you: copy the
REM GameData\AutoStrutFixMod folder into the GameData of KSP.
setlocal
cd /d "%~dp0"

if not defined KSPDIR (
    echo ERROR: KSPDIR is not set. Point it at your KSP install folder.
    exit /b 1
)

dotnet build AutoStrutFixMod.csproj -c Release -p:KSPDIR="%KSPDIR%"
exit /b %errorlevel%
