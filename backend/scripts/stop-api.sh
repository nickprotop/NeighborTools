#!/bin/bash

# Stop API service (both Docker and dotnet processes)
# Keeps infrastructure running for development

set -e  # Exit on any error

echo "🛑 Stopping NeighborTools API"
echo "============================="

api_stopped=false

# Check and stop Docker API container
cd "$(dirname "$0")/../docker"
if docker-compose ps api | grep -q "Up"; then
    echo "🔄 Stopping API Docker container..."
    docker-compose stop api
    echo "✅ API Docker container stopped"
    api_stopped=true
fi

# Check and stop dotnet processes
echo "🔍 Checking for running dotnet API processes..."
api_pids=$(pgrep -f "ToolsSharing.API" || true)

if [ -n "$api_pids" ]; then
    echo "🔄 Stopping dotnet API processes..."
    echo "$api_pids" | xargs kill -TERM
    sleep 2
    
    # Force kill if still running
    remaining_pids=$(pgrep -f "ToolsSharing.API" || true)
    if [ -n "$remaining_pids" ]; then
        echo "🔄 Force stopping remaining processes..."
        echo "$remaining_pids" | xargs kill -9
    fi
    
    echo "✅ Dotnet API processes stopped"
    api_stopped=true
fi

if [ "$api_stopped" = false ]; then
    echo "ℹ️  No API processes found running"
fi

echo ""
echo "Infrastructure services remain running:"
echo "  • MySQL: localhost:3306"
echo "  • Redis: localhost:6379"
echo ""
echo "To restart API:"
echo "  • Docker mode: ./docker-api.sh"
echo "  • Development mode: ./dev-api.sh"
echo "  • Interactive choice: ./start-all.sh"