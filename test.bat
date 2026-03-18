@echo off
REM EasyMeeting Test Script

echo Running EasyMeeting tests...
cd /d "%~dp0"

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet CLI not found. Please install .NET 8 SDK.
    exit /b 1
)

dotnet test tests\EasyMeeting.Tests\EasyMeeting.Tests.csproj -v n
