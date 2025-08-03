#!/bin/bash

# Show storage services status

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Calculate absolute paths before any directory changes
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../../docker" && pwd)"

echo "📊 Storage Services Status"
echo "=========================="

# Navigate to docker directory
cd "$DOCKER_DIR"

echo "🐳 Docker Compose Status:"
if docker-compose ps --services --filter "status=running" | grep -q "postgresql"; then
    echo "   PostgreSQL: ✅ Running"
else
    echo "   PostgreSQL: ❌ Not running"
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

# Restore original directory
cd "$ORIGINAL_DIR"

echo ""
echo "🔗 Service URLs (when running):"
echo "   • PostgreSQL: localhost:5433"
echo "   • Redis: localhost:6379"
echo "   • MinIO API: http://localhost:9000"
echo "   • MinIO Console: http://localhost:9001"