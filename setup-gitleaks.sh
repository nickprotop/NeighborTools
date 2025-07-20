#!/bin/bash

# GitLeaks Setup Script for NeighborTools
# This script helps new developers set up GitLeaks on their machine

echo "🔐 NeighborTools GitLeaks Setup"
echo "================================"

# Check if GitLeaks is already installed
if command -v gitleaks &> /dev/null; then
    echo "✅ GitLeaks is already installed!"
    gitleaks version
    echo ""
else
    echo "❌ GitLeaks not found. Installing..."
    
    # Detect OS and install GitLeaks
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "📦 Installing GitLeaks via apt (Ubuntu/Debian)..."
        sudo apt update && sudo apt install -y gitleaks
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        echo "📦 Installing GitLeaks via Homebrew (macOS)..."
        if command -v brew &> /dev/null; then
            brew install gitleaks
        else
            echo "❌ Homebrew not found. Please install Homebrew first:"
            echo "   /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            exit 1
        fi
    else
        echo "❌ Unsupported OS. Please install GitLeaks manually:"
        echo "   https://github.com/gitleaks/gitleaks#installation"
        echo ""
        echo "🐳 Alternative: Use Docker-based hook (no local installation needed)"
        echo "   cp .git/hooks/pre-commit-docker .git/hooks/pre-commit"
        echo "   chmod +x .git/hooks/pre-commit"
        exit 1
    fi
fi

# Verify installation
echo ""
echo "🔍 Verifying GitLeaks installation..."
if gitleaks version; then
    echo "✅ GitLeaks installed successfully!"
else
    echo "❌ GitLeaks installation failed. Falling back to Docker option..."
    echo ""
    echo "🐳 Setting up Docker-based GitLeaks hook..."
    if [ -f ".git/hooks/pre-commit-docker" ]; then
        cp .git/hooks/pre-commit-docker .git/hooks/pre-commit
        chmod +x .git/hooks/pre-commit
        echo "✅ Docker-based GitLeaks hook configured!"
    else
        echo "❌ Docker hook not found. Please check repository setup."
        exit 1
    fi
fi

# Check hook setup
echo ""
echo "🪝 Checking pre-commit hook..."
if [ -f ".git/hooks/pre-commit" ] && [ -x ".git/hooks/pre-commit" ]; then
    echo "✅ Pre-commit hook is configured and executable"
else
    echo "❌ Pre-commit hook not found or not executable"
    echo "   Please check .git/hooks/pre-commit"
fi

# Test the setup
echo ""
echo "🧪 Testing GitLeaks setup..."
echo "test-content" > .gitleaks-test.tmp
git add .gitleaks-test.tmp 2>/dev/null || true

echo "Running GitLeaks test scan..."
if ./.git/hooks/pre-commit; then
    echo "✅ GitLeaks test completed successfully!"
else
    echo "❌ GitLeaks test failed. Please check configuration."
fi

# Cleanup
rm -f .gitleaks-test.tmp
git reset .gitleaks-test.tmp 2>/dev/null || true

echo ""
echo "🎉 GitLeaks setup complete!"
echo ""
echo "📋 What happens now:"
echo "   • Every git commit will be automatically scanned for secrets"
echo "   • If secrets are detected, the commit will be blocked"
echo "   • GitHub Actions will also scan on push/PR"
echo ""
echo "🔗 Useful commands:"
echo "   gitleaks detect --verbose              # Scan entire repository"
echo "   gitleaks protect --verbose             # Scan staged changes only"
echo "   git commit --no-verify -m \"...\"        # Bypass hook (emergency only!)"
echo ""
echo "📚 Documentation: See README.md section '🔐 Secret Scanning with GitLeaks'"