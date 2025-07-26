#!/bin/bash

# Start API locally with dotnet run (stable development)

set -e  # Exit on any error

echo "💻 Starting API with dotnet run"
echo "================================"

# Check if storage services are running
echo "🔍 Checking storage services..."
DOCKER_DIR="$(dirname "$0")/../../docker"
cd "$DOCKER_DIR"

MISSING_SERVICES=""
if ! docker-compose ps --services --filter "status=running" | grep -q "mysql"; then
    MISSING_SERVICES="$MISSING_SERVICES MySQL"
fi
if ! docker-compose ps --services --filter "status=running" | grep -q "redis"; then
    MISSING_SERVICES="$MISSING_SERVICES Redis"
fi
if ! docker-compose ps --services --filter "status=running" | grep -q "minio"; then
    MISSING_SERVICES="$MISSING_SERVICES MinIO"
fi

if [ -n "$MISSING_SERVICES" ]; then
    echo "❌ Storage services not running:$MISSING_SERVICES"
    echo "💡 Start storage first: ./storage/start.sh"
    echo "   Or use complete workflow: ./start-dev.sh"
    exit 1
fi

echo "✅ Storage services are running"

# Navigate to API directory
cd "../../src/ToolsSharing.API"

echo ""
echo "🚀 Starting API with dotnet run..."
echo "🌐 API will be available at: http://localhost:5002"
echo "📖 Swagger: http://localhost:5002/swagger"
echo "📁 MinIO Console: http://localhost:9001"
echo ""
echo "Press Ctrl+C to stop"
echo ""

dotnet run