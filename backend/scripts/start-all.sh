#!/bin/bash

# NeighborTools Development Environment Starter
# Daily development workflow - choose your API mode

set -e  # Exit on any error

echo "🚀 Starting NeighborTools Development Environment"
echo "================================================="

# Check if infrastructure is already running
cd "$(dirname "$0")/../docker"

# Start infrastructure
echo "📦 Starting infrastructure (MySQL, Redis)..."
docker-compose --profile infrastructure up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 3

# Check if services are running
if ! docker-compose exec -T mysql mysqladmin ping -h localhost --silent; then
    echo "❌ MySQL is not ready. Please check the logs with: docker-compose logs mysql"
    exit 1
fi

if ! docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
    echo "❌ Redis is not ready. Please check the logs with: docker-compose logs redis"
    exit 1
fi

echo "✅ Infrastructure is ready"

# Navigate back to backend root
cd ..

# Detect preferred mode
preferred_mode=""
if [ -f ".dev-mode" ]; then
    preferred_mode=$(cat .dev-mode)
    echo "🔍 Found preferred mode: $preferred_mode"
fi

# API mode selection
echo ""
echo "🔧 Choose API mode:"
echo "1) Docker     (recommended for production-like testing)"
echo "2) dotnet run (recommended for development/debugging)"
echo "3) dotnet watch (hot reload for active development)"

if [ -n "$preferred_mode" ]; then
    echo ""
    echo "Press Enter to use preferred mode ($preferred_mode) or select a number:"
    read -t 5 mode || mode=""
    if [ -z "$mode" ]; then
        case "$preferred_mode" in
            "docker") mode="1" ;;
            "dotnet") mode="2" ;;
            "watch") mode="3" ;;
        esac
    fi
else
    echo ""
    read -p "Select mode [1-3]: " mode
fi

# Remember choice
case $mode in
    1)
        echo "docker" > .dev-mode
        echo "🐳 Starting API in Docker mode..."
        cd docker
        docker-compose --profile api up -d
        echo ""
        echo "✅ API started in Docker mode"
        echo "🌐 API URL: http://localhost:5002"
        echo "📖 Swagger: http://localhost:5002/swagger"
        echo "📊 Logs: docker-compose logs -f api"
        ;;
    2)
        echo "dotnet" > .dev-mode
        echo "💻 Starting API with dotnet run..."
        echo "🌐 API will be available at: http://localhost:5000"
        echo "📖 Swagger: http://localhost:5000/swagger"
        echo ""
        cd src/ToolsSharing.API
        dotnet run
        ;;
    3)
        echo "watch" > .dev-mode
        echo "🔥 Starting API with hot reload (dotnet watch)..."
        echo "🌐 API will be available at: http://localhost:5000"
        echo "📖 Swagger: http://localhost:5000/swagger"
        echo "🔄 Hot reload enabled - changes will auto-restart"
        echo ""
        cd src/ToolsSharing.API
        dotnet watch run
        ;;
    *)
        echo "❌ Invalid choice. Please run the script again."
        exit 1
        ;;
esac