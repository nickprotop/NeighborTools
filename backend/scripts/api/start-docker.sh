#!/bin/bash

# Start API in Docker container (production-like testing)

set -e  # Exit on any error

echo "🐳 Starting API in Docker"
echo "=========================="

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
    echo "   Or use complete workflow: ./start-production.sh"
    exit 1
fi

echo "✅ Storage services are running"

# Start API container
echo "🔄 Starting API container..."
docker-compose --profile api up -d

echo ""
echo "✅ API started in Docker"
echo "========================"
echo "🌐 API URL: http://localhost:5002"
echo "📖 Swagger: http://localhost:5002/swagger"
echo "📁 MinIO Console: http://localhost:9001"
echo "📊 Logs: docker-compose logs -f api"
echo ""
echo "To stop: ./api/stop.sh"