#!/bin/bash

# Start API with dotnet watch (hot reload for active development)

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Calculate absolute paths before any directory changes
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../../docker" && pwd)"
API_DIR="$(cd "$SCRIPT_DIR/../../src/ToolsSharing.API" && pwd)"

echo "🔥 Starting API with dotnet watch (hot reload)"
echo "==============================================="

# Check if storage services are running
echo "🔍 Checking storage services..."
cd "$DOCKER_DIR"

MISSING_SERVICES=""
if ! docker-compose ps --services --filter "status=running" | grep -q "postgresql"; then
    MISSING_SERVICES="$MISSING_SERVICES PostgreSQL"
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
    echo "   Or use complete workflow: ./start-watch.sh"
    cd "$ORIGINAL_DIR"
    exit 1
fi

echo "✅ Storage services are running"

# Navigate to API directory
cd "$API_DIR"

echo ""
echo "🚀 Starting API with hot reload..."
echo "🌐 API will be available at: http://localhost:5002"
echo "📖 Swagger: http://localhost:5002/swagger"
echo "📁 MinIO Console: http://localhost:9001"
echo "🔄 Hot reload enabled - changes will auto-restart"
echo ""
echo "Press Ctrl+C to stop"
echo ""

# Restore original directory on exit
trap "cd '$ORIGINAL_DIR'" EXIT

dotnet watch run