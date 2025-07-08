#!/bin/bash

# Installation script for Tools Sharing Backend

echo "===================================================="
echo "Tools Sharing Backend Installation Script"
echo "===================================================="

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "Checking prerequisites..."

# Check for .NET SDK
if ! command_exists dotnet; then
    echo "Error: .NET SDK 9.0 is required but not installed."
    echo "Please install .NET SDK 9.0 from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "Found .NET SDK version: $DOTNET_VERSION"

# Check for Docker
if ! command_exists docker; then
    echo "Error: Docker is required but not installed."
    echo "Please install Docker from: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check for Docker Compose
if ! command_exists docker-compose; then
    echo "Error: Docker Compose is required but not installed."
    echo "Please install Docker Compose from: https://docs.docker.com/compose/install/"
    exit 1
fi

echo "All prerequisites satisfied!"

# Navigate to backend directory
cd "$(dirname "$0")/.."

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore ToolsSharing.sln

if [ $? -ne 0 ]; then
    echo "Error: Failed to restore NuGet packages"
    exit 1
fi

# Build the solution
echo "Building the solution..."
dotnet build ToolsSharing.sln --configuration Release

if [ $? -ne 0 ]; then
    echo "Error: Failed to build the solution"
    exit 1
fi

# Install EF Core tools
echo "Installing Entity Framework Core tools..."
dotnet tool install --global dotnet-ef --version 9.0.6 || dotnet tool update --global dotnet-ef --version 9.0.6

# Make scripts executable
echo "Setting up script permissions..."
chmod +x scripts/*.sh

echo "===================================================="
echo "Installation completed successfully!"
echo "===================================================="
echo ""
echo "Next steps:"
echo "1. Start the infrastructure: ./scripts/start-infrastructure.sh"
echo "2. Run database migrations: ./scripts/run-migrations.sh"
echo "3. Seed the database: ./scripts/seed-data.sh"
echo "4. Start the API: ./scripts/start-api.sh"
echo ""
echo "Or run everything at once: ./scripts/run-all.sh"