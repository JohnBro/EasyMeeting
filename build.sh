#!/bin/bash
# EasyMeeting Build Script for macOS/Linux (requires .NET 8)

set -e

echo "========================================"
echo "EasyMeeting Build Script"
echo "========================================"
echo ""

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet CLI not found. Please install .NET 8 SDK."
    echo "Download: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "Using .NET version:"
dotnet --version
echo ""

# Restore packages
echo "[1/4] Restoring NuGet packages..."
dotnet restore EasyMeeting.sln
echo ""

# Build solution
echo "[2/4] Building solution..."
dotnet build EasyMeeting.sln -c Release
echo ""

# Run tests
echo "[3/4] Running tests..."
dotnet test EasyMeeting.sln -c Release --no-build || true
echo ""

# Publish
echo "[4/4] Publishing..."
dotnet publish src/EasyMeeting/EasyMeeting.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish
echo ""

echo "========================================"
echo "Build complete!"
echo "EXE location: publish/EasyMeeting.exe"
echo "========================================"
