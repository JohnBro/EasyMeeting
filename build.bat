@echo off
REM EasyMeeting Build Script for Windows
REM Requires .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

echo ========================================
echo EasyMeeting Build Script
echo ========================================
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet CLI not found. Please install .NET 8 SDK.
    echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

REM Show dotnet version
echo Using .NET version:
dotnet --version
echo.

REM Restore packages
echo [1/4] Restoring NuGet packages...
dotnet restore EasyMeeting.sln
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Package restore failed.
    exit /b 1
)
echo.

REM Build solution
echo [2/4] Building solution...
dotnet build EasyMeeting.sln -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed.
    exit /b 1
)
echo.

REM Run tests
echo [3/4] Running tests...
dotnet test EasyMeeting.sln -c Release --no-build
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Some tests failed.
)
echo.

REM Publish
echo [4/4] Publishing self-contained EXE...
dotnet publish src\EasyMeeting\EasyMeeting.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed.
    exit /b 1
)
echo.

echo ========================================
echo Build complete!
echo EXE location: publish\EasyMeeting.exe
echo ========================================
echo.
echo To run the application:
echo   .\publish\EasyMeeting.exe
echo.
pause
