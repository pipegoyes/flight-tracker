#!/bin/bash
# Setup script for Playwright browser automation tests
# Installs Chromium browser for headless testing on Linux

set -e

export PATH="$HOME/.dotnet:$PATH"

echo "🎭 Playwright Browser Setup"
echo "==========================="
echo ""

# Build the test project first
echo "📦 Building test project..."
cd "$(dirname "$0")"
dotnet build > /dev/null 2>&1

# Find the playwright CLI
PLAYWRIGHT_DIR="bin/Debug/net8.0/.playwright/package"

if [ ! -d "$PLAYWRIGHT_DIR" ]; then
    echo "❌ Playwright package not found. Did the build succeed?"
    exit 1
fi

echo "✅ Found Playwright package"
echo ""

# Check if node is available
if command -v node &> /dev/null; then
    echo "📥 Installing Chromium browser..."
    node "$PLAYWRIGHT_DIR/cli.js" install chromium --with-deps
    echo "✅ Chromium installed successfully!"
else
    echo "⚠️  Node.js not found. Trying alternative method..."
    
    # Try using the .NET playwright driver directly
    if [ -f "bin/Debug/net8.0/.playwright/node/linux/playwright.sh" ]; then
        ./bin/Debug/net8.0/.playwright/node/linux/playwright.sh install chromium
        echo "✅ Chromium installed!"
    else
        echo "❌ Could not install browsers automatically."
        echo ""
        echo "Manual installation:"
        echo "1. Install Node.js: sudo apt-get install nodejs"
        echo "2. Run: node bin/Debug/net8.0/.playwright/package/lib/cli/cli.js install chromium"
        exit 1
    fi
fi

echo ""
echo "🎯 Ready to run Playwright tests!"
echo ""
echo "Run tests with:"
echo "  dotnet test --filter PlaywrightUITests"
echo ""
