@echo off
REM EasyMeeting Development Script
REM Runs the application in development mode

echo Starting EasyMeeting in development mode...
cd /d "%~dp0"

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet CLI not found. Please install .NET 8 SDK.
    exit /b 1
)

dotnet run --project src\EasyMeeting\EasyMeeting.csproj
