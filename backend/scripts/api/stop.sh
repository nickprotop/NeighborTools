#!/bin/bash

# Stop API (handles both Docker and dotnet processes)

echo "🛑 Stopping API"
echo "==============="

# Navigate to docker directory
DOCKER_DIR="$(dirname "$0")/../../docker"
cd "$DOCKER_DIR"

# Check if API container is running
if docker-compose ps --services --filter "status=running" | grep -q "api"; then
    echo "🐳 Stopping API Docker container..."
    docker-compose --profile api stop
    echo "✅ API Docker container stopped"
else
    echo "ℹ️  No API Docker container running"
fi

# Kill any dotnet processes running the API
API_PROCESSES=$(pgrep -f "ToolsSharing.API" || true)
if [ -n "$API_PROCESSES" ]; then
    echo "💻 Stopping local dotnet API processes..."
    pkill -f "ToolsSharing.API" || true
    echo "✅ Local API processes stopped"
else
    echo "ℹ️  No local API processes running"
fi

echo "✅ API stopped"