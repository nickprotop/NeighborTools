#!/bin/bash

# Start storage services (MySQL, Redis, MinIO) in Docker
# These services are always containerized for consistency

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Calculate absolute paths before any directory changes
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../../docker" && pwd)"
ENV_FILE="$DOCKER_DIR/.env"

echo "📦 Starting NeighborTools Storage Services"
echo "==========================================="

# Load environment variables from .env file

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
cd "$DOCKER_DIR"

# Start only infrastructure services
echo "🔄 Starting MySQL, Redis, and MinIO..."
docker-compose --profile infrastructure up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 5

# Check MySQL
if docker-compose exec -T mysql mysqladmin ping -h localhost --silent; then
    echo "✅ MySQL is ready (localhost:3306)"
else
    echo "❌ MySQL is not ready. Check logs: docker-compose logs mysql"
    exit 1
fi

# Check Redis (with password authentication if enabled)
if [ "${ENABLE_REDIS_PASSWORD:-false}" = "true" ] && [ -n "${REDIS_PASSWORD:-}" ]; then
    if docker-compose exec -T redis redis-cli -a "$REDIS_PASSWORD" ping | grep -q "PONG"; then
        echo "✅ Redis is ready with authentication (localhost:6379)"
    else
        echo "❌ Redis is not ready with authentication. Check logs: docker-compose logs redis"
        exit 1
    fi
else
    if docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
        echo "✅ Redis is ready (localhost:6379)"
    else
        echo "❌ Redis is not ready. Check logs: docker-compose logs redis"
        exit 1
    fi
fi

# Check MinIO (use curl to check health endpoint)
if curl -s http://localhost:9000/minio/health/live > /dev/null 2>&1; then
    echo "✅ MinIO is ready (API: localhost:9000, Console: localhost:9001)"
else
    echo "❌ MinIO is not ready. Check logs: docker-compose logs minio"
    exit 1
fi

# Restore original directory
cd "$ORIGINAL_DIR"

echo ""
echo "🎉 Storage services are ready!"
echo "==============================="
echo "Services available:"
echo "  • MySQL: localhost:3306"
echo "  • Redis: localhost:6379"
echo "  • MinIO API: http://localhost:9000"
echo "  • MinIO Console: http://localhost:9001"
echo ""
echo "Next: Start API with ./api/start-local.sh, ./api/start-watch.sh, or ./api/start-docker.sh"
echo "Or use complete workflows: ./start-dev.sh, ./start-watch.sh, or ./start-production.sh"