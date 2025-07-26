#!/bin/bash

# Show storage services status

echo "📊 Storage Services Status"
echo "=========================="

# Navigate to docker directory
DOCKER_DIR="$(dirname "$0")/../../docker"
cd "$DOCKER_DIR"

echo "🐳 Docker Compose Status:"
if docker-compose ps --services --filter "status=running" | grep -q "mysql"; then
    echo "   MySQL: ✅ Running"
else
    echo "   MySQL: ❌ Not running"
fi

if docker-compose ps --services --filter "status=running" | grep -q "redis"; then
    echo "   Redis: ✅ Running"
else
    echo "   Redis: ❌ Not running"
fi

if docker-compose ps --services --filter "status=running" | grep -q "minio"; then
    echo "   MinIO: ✅ Running (Console: http://localhost:9001)"
else
    echo "   MinIO: ❌ Not running"
fi

echo ""
echo "🔗 Service URLs (when running):"
echo "   • MySQL: localhost:3306"
echo "   • Redis: localhost:6379"
echo "   • MinIO API: http://localhost:9000"
echo "   • MinIO Console: http://localhost:9001"