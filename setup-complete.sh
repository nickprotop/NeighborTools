#!/bin/bash

# NeighborTools Complete Development Environment Setup
# This script sets up both backend and frontend for development

set -e  # Exit on any error

echo "🚀 NeighborTools Complete Development Setup"
echo "============================================"
echo ""
echo "This script will set up your complete development environment:"
echo "  🔧 Backend: Database, storage services, and API configuration"
echo "  🎨 Frontend: Blazor WebAssembly application configuration"
echo "  🐳 Infrastructure: Docker containers for MySQL, Redis, MinIO"
echo ""
echo "Prerequisites:"
echo "  ✅ Docker and Docker Compose"
echo "  ✅ .NET 9 SDK"
echo "  ✅ Git (for development)"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "🔍 Checking prerequisites..."
missing_deps=()

if ! command_exists docker; then
    missing_deps+=("Docker")
fi

if ! command_exists docker-compose; then
    missing_deps+=("Docker Compose")
fi

if ! command_exists dotnet; then
    missing_deps+=(".NET 9 SDK")
fi

if [ ${#missing_deps[@]} -gt 0 ]; then
    echo "❌ Missing prerequisites:"
    for dep in "${missing_deps[@]}"; do
        echo "   • $dep"
    done
    echo ""
    echo "Please install missing prerequisites and run this script again."
    echo ""
    echo "Installation links:"
    echo "  • Docker: https://docs.docker.com/get-docker/"
    echo "  • .NET 9 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ Prerequisites check passed"
echo ""

# Check if we're in the right directory
if [ ! -d "backend" ] || [ ! -d "frontend" ]; then
    echo "❌ This script must be run from the NeighborTools root directory"
    echo "   Expected structure:"
    echo "   NeighborTools/"
    echo "   ├── backend/"
    echo "   └── frontend/"
    echo ""
    echo "Current directory: $(pwd)"
    exit 1
fi

echo "✅ Directory structure verified"
echo ""

# Confirmation
echo -n "Continue with complete setup? [Y/n]: "
read -r confirm
echo ""

if [[ "$confirm" =~ ^[Nn]$ ]]; then
    echo "❌ Setup cancelled."
    exit 0
fi

# Step 1: Backend Setup
echo "🔧 Step 1: Backend Setup"
echo "========================"
echo "Setting up backend infrastructure, database, and API configuration..."
echo ""

cd backend
if [ -f "scripts/install.sh" ]; then
    ./scripts/install.sh
else
    echo "❌ Backend install script not found at: backend/scripts/install.sh"
    exit 1
fi
cd ..

echo ""
echo "✅ Backend setup complete!"
echo ""

# Step 2: Frontend Configuration
echo "🎨 Step 2: Frontend Configuration"
echo "================================="
echo "Configuring Blazor WebAssembly application..."
echo ""

cd frontend

# Check if configure.sh exists
if [ ! -f "configure.sh" ]; then
    echo "❌ Frontend configure.sh not found"
    exit 1
fi

# Use the frontend's own configuration script in interactive mode
echo "🔧 Running frontend configuration (interactive mode)..."
echo "   You'll be prompted to configure API URL, environment, and home page URL"
echo "   Press Enter to accept defaults for development setup"
echo ""
./configure.sh

echo ""
echo "📦 Restoring frontend dependencies..."
dotnet restore
echo "✅ Frontend dependencies restored"

cd ..

echo ""
echo "✅ Frontend configuration complete!"
echo ""

# Step 3: Verify Infrastructure
echo "🐳 Step 3: Infrastructure Verification"
echo "======================================"
echo "Verifying that infrastructure services are running..."
echo ""

cd backend

# Check if infrastructure is running
if [ -f "scripts/storage/status.sh" ]; then
    ./scripts/storage/status.sh
else
    echo "⚠️  Infrastructure status script not found"
    echo "   Manually check with: cd backend && ./scripts/storage/status.sh"
fi

cd ..

echo ""

# Step 4: Display final instructions
# Step 4: Development Machine Setup
echo ""
echo "🔧 Step 4: Development Environment Setup"
echo "========================================"
echo ""
echo "Are you setting up this environment for development? [Y/n]: "
read -r is_dev_machine

if [[ ! "$is_dev_machine" =~ ^[Nn]$ ]]; then
    echo ""
    echo "🔒 Git Security Setup"
    echo "===================="
    echo "For development environments, we recommend setting up GitLeaks"
    echo "to prevent accidentally committing secrets (passwords, API keys, etc.)"
    echo ""
    echo "GitLeaks will:"
    echo "  • Scan commits for secrets before they're pushed"
    echo "  • Prevent accidental exposure of sensitive data"
    echo "  • Add pre-commit hooks for automatic scanning"
    echo ""
    echo "Would you like to set up GitLeaks now? [Y/n]: "
    read -r setup_gitleaks
    
    if [[ ! "$setup_gitleaks" =~ ^[Nn]$ ]]; then
        echo ""
        echo "🔧 Setting up GitLeaks..."
        if [ -f "setup-gitleaks.sh" ]; then
            ./setup-gitleaks.sh
            echo "✅ GitLeaks setup complete!"
        else
            echo "❌ GitLeaks setup script not found at: setup-gitleaks.sh"
            echo "   You can run it manually later if needed"
        fi
    else
        echo "⚠️  GitLeaks setup skipped"
        echo "   You can run it later with: ./setup-gitleaks.sh"
    fi
else
    echo "ℹ️  Production/staging environment detected"
    echo "   Skipping development-specific setup"
fi

echo ""
echo "🎉 Complete Setup Finished!"
echo "==========================="
echo ""
echo "📋 Your development environment is ready!"
echo ""
echo "🚀 To start development:"
echo ""
echo "Option 1 - Full development with hot reload (recommended):"
echo "  cd backend && ./start-watch.sh"
echo "  # In another terminal:"
echo "  cd frontend && dotnet run"
echo ""
echo "Option 2 - Stable development (good for debugging):"
echo "  cd backend && ./start-dev.sh  "
echo "  # In another terminal:"
echo "  cd frontend && dotnet run"
echo ""
echo "Option 3 - Production-like testing:"
echo "  cd backend && ./start-production.sh"
echo "  # In another terminal:"
echo "  cd frontend && dotnet run"
echo ""
echo "🌐 Application URLs:"
echo "  • Frontend: http://localhost:5000"
echo "  • Backend API: http://localhost:5002"
echo "  • Swagger UI: http://localhost:5002/swagger"
echo "  • MinIO Console: http://localhost:9001"
echo ""
echo "👤 Default Admin Account:"
echo "  • Email: admin@neighbortools.com"
echo "  • Password: Admin123!"
echo ""
echo "🔧 Useful Commands:"
echo "  • View configuration: cd backend && ./scripts/show-config.sh"
echo "  • Stop all services: cd backend && ./scripts/api/stop.sh && ./scripts/storage/stop.sh"
echo "  • Reset database: cd backend && ./scripts/uninstall.sh && ./scripts/install.sh"
echo ""
if [[ ! "$is_dev_machine" =~ ^[Nn]$ ]]; then
    echo "🔒 Security:"
    echo "  • GitLeaks prevents secret commits (if installed)"
    echo "  • Run ./setup-gitleaks.sh if you skipped it"
    echo ""
fi
echo "📚 Documentation:"
echo "  • Backend scripts: backend/scripts/README.md"
echo "  • Project documentation: CLAUDE.md"
echo ""
echo "Happy coding! 🚀"