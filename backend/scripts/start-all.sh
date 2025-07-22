#!/bin/bash

# NeighborTools Development Environment Starter
# Daily development workflow - choose your API mode

set -e  # Exit on any error

echo "🚀 Starting NeighborTools Development Environment"
echo "================================================="

# Load environment variables from .env file
DOCKER_DIR="$(dirname "$0")/../docker"
ENV_FILE="$DOCKER_DIR/.env"

if [ -f "$ENV_FILE" ]; then
    echo "📝 Loading configuration from .env file..."
    set -a  # Automatically export variables
    source "$ENV_FILE"
    set +a  # Stop auto-export
    echo "✅ Configuration loaded from .env file"
else
    echo "⚠️  No .env file found. Using default passwords."
    echo "   Run ./install.sh to configure custom passwords."
    # Create .env file with defaults
    if [ -f "$DOCKER_DIR/.env.sample" ]; then
        cp "$DOCKER_DIR/.env.sample" "$ENV_FILE"
        echo "✅ Created .env file from sample"
    fi
fi

# Check if infrastructure is already running
cd "$(dirname "$0")/../docker"

# Start infrastructure
echo "📦 Starting infrastructure (MySQL, Redis, MinIO)..."
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

# Check MinIO (use curl to check health endpoint)
if ! curl -s http://localhost:9000/minio/health/live > /dev/null 2>&1; then
    echo "❌ MinIO is not ready. Please check the logs with: docker-compose logs minio"
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
        echo "📁 MinIO Console: http://localhost:9001"
        echo "📊 Logs: docker-compose logs -f api"
        ;;
    2)
        echo "dotnet" > .dev-mode
        echo "💻 Starting API with dotnet run..."
        echo "🌐 API will be available at: http://localhost:5002"
        echo "📖 Swagger: http://localhost:5002/swagger"
        echo "📁 MinIO Console: http://localhost:9001"
        echo ""
        cd src/ToolsSharing.API
        dotnet run
        ;;
    3)
        echo "watch" > .dev-mode
        echo "🔥 Starting API with hot reload (dotnet watch)..."
        echo "🌐 API will be available at: http://localhost:5000"
        echo "📖 Swagger: http://localhost:5002/swagger"
        echo "📁 MinIO Console: http://localhost:9001"
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