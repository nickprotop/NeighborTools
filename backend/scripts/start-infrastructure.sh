#!/bin/bash

# Start only infrastructure services (MySQL, Redis)
# Use this when you want to run the API manually with dotnet run

set -e  # Exit on any error

echo "📦 Starting NeighborTools Infrastructure"
echo "========================================"

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

# Navigate to docker directory
cd "$(dirname "$0")/../docker"

# Start only infrastructure services
echo "🔄 Starting MySQL and Redis..."
docker-compose --profile infrastructure up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 3

# Check MySQL
if docker-compose exec -T mysql mysqladmin ping -h localhost --silent; then
    echo "✅ MySQL is ready (localhost:3306)"
else
    echo "❌ MySQL is not ready. Check logs: docker-compose logs mysql"
    exit 1
fi

# Check Redis
if docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
    echo "✅ Redis is ready (localhost:6379)"
else
    echo "❌ Redis is not ready. Check logs: docker-compose logs redis"
    exit 1
fi

echo ""
echo "🎉 Infrastructure is ready!"
echo "========================================"
echo "Next steps:"
echo "  • Run API manually: cd src/ToolsSharing.API && dotnet run"
echo "  • Or with hot reload: cd src/ToolsSharing.API && dotnet watch run"
echo "  • API will be available at: http://localhost:5002"
echo "  • Swagger UI: http://localhost:5002/swagger"
echo ""
echo "To stop infrastructure: docker-compose down"