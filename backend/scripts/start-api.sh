#!/bin/bash

# Script to start the API server

echo "Starting Tools Sharing API..."

cd "$(dirname "$0")/../src/ToolsSharing.API"

# Check if the project file exists
if [ ! -f "ToolsSharing.API.csproj" ]; then
    echo "Error: ToolsSharing.API.csproj not found"
    exit 1
fi

# Start the API
echo "Starting API server..."
echo "API will be available at:"
echo "- HTTP: http://0.0.0.0:5000 (or http://localhost:5000)"
echo "- HTTPS: https://0.0.0.0:5001 (or https://localhost:5001)"
echo "- Swagger UI: http://0.0.0.0:5000/swagger (or http://localhost:5000/swagger)"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

dotnet run --configuration Release

if [ $? -ne 0 ]; then
    echo "Error: Failed to start API server"
    exit 1
fi