#!/bin/bash
# Quick Start Script for Readarr with OpenLibrary
# This builds only the backend (no frontend required for basic use)

set -e

echo "=================================="
echo "Readarr with OpenLibrary"
echo "Quick Start Build"
echo "=================================="
echo ""

# Check for .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK not found"
    echo "Please install .NET 6.0 or later from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Build backend only (faster, no frontend needed)
echo "🔨 Building backend (this may take a few minutes)..."
./build.sh --backend

if [ $? -eq 0 ]; then
    echo ""
    echo "=================================="
    echo "✅ Build Complete!"
    echo "=================================="
    echo ""
    echo "🚀 To start Readarr with OpenLibrary:"
    echo ""
    echo "  ./_output/net6.0/linux-x64/Readarr/Readarr --nobrowser"
    echo ""
    echo "  Or on macOS:"
    echo "  ./_output/net6.0/osx-x64/Readarr/Readarr --nobrowser"
    echo ""
    echo "Then open your browser to the URL shown in the console"
    echo "(usually http://localhost:8787)"
    echo ""
    echo "📚 OpenLibrary metadata will be used automatically!"
    echo ""
else
    echo ""
    echo "❌ Build failed - see errors above"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure you have internet connectivity"
    echo "  - Check that .NET 6.0+ is installed"
    echo "  - Try: cd src && dotnet clean && dotnet restore"
    exit 1
fi
