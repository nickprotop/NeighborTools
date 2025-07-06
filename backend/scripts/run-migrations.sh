#!/bin/bash

# Script to run Entity Framework migrations

echo "Starting database migration process..."

# Navigate to the API project directory
cd "$(dirname "$0")/../src/ToolsSharing.API"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH"
    exit 1
fi

# Check if the project file exists
if [ ! -f "ToolsSharing.API.csproj" ]; then
    echo "Error: ToolsSharing.API.csproj not found"
    exit 1
fi

# Install EF Core tools if not already installed
echo "Installing/updating Entity Framework Core tools..."
dotnet tool install --global dotnet-ef --version 9.0.6 || dotnet tool update --global dotnet-ef --version 9.0.6

# Check if migrations exist
echo "Checking for existing migrations..."
if [ -d "../ToolsSharing.Infrastructure/Migrations" ] && [ "$(ls -A ../ToolsSharing.Infrastructure/Migrations/*.cs 2>/dev/null | wc -l)" -gt 0 ]; then
    echo "Migrations already exist, skipping initial migration creation..."
else
    echo "No migrations found. Adding initial migration..."
    dotnet ef migrations add InitialCreate --project ../ToolsSharing.Infrastructure/ToolsSharing.Infrastructure.csproj --startup-project . || echo "Migration creation failed"
fi

# Update database
echo "Updating database..."
dotnet ef database update --project ../ToolsSharing.Infrastructure/ToolsSharing.Infrastructure.csproj --startup-project .

if [ $? -eq 0 ]; then
    echo "Database migration completed successfully!"
else
    echo "Error: Database migration failed"
    exit 1
fi

echo "Migration process finished."