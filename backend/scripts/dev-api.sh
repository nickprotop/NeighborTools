#!/bin/bash

# Start API in development mode (dotnet run)
# Used by start-all.sh when dotnet mode is selected

set -e  # Exit on any error

echo "💻 Starting API in development mode..."
echo "======================================="

# Navigate to API project directory
cd "$(dirname "$0")/../src/ToolsSharing.API"

# Check if infrastructure is running
echo "🔍 Checking infrastructure..."
if ! docker ps | grep -q "mysql.*Up" || ! docker ps | grep -q "redis.*Up"; then
    echo "❌ Infrastructure is not running. Please run start-infrastructure-new.sh first"
    exit 1
fi

echo "✅ Infrastructure is ready"
echo ""
echo "🚀 Starting API with dotnet run..."
echo "🌐 API will be available at: http://localhost:5000"
echo "📖 Swagger UI: http://localhost:5000/swagger"
echo "🔄 Press Ctrl+C to stop"
echo ""

# Start the API
dotnet run