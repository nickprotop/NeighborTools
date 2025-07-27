#!/bin/bash

# Stop API (handles both Docker and dotnet processes)

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Calculate absolute paths before any directory changes
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../../docker" && pwd)"

echo "🛑 Stopping API"
echo "==============="

# Navigate to docker directory
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

# Restore original directory
cd "$ORIGINAL_DIR"

echo "✅ API stopped"