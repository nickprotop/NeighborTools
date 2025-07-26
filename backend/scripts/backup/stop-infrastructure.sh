#!/bin/bash

# Stop infrastructure services (MySQL, Redis, MinIO)
# Use when you want to stop development environment

set -e  # Exit on any error

echo "🛑 Stopping NeighborTools Infrastructure"
echo "========================================"

# Navigate to docker directory
cd "$(dirname "$0")/../docker"

# Stop infrastructure services
echo "🔄 Stopping MySQL, Redis, and MinIO..."
docker-compose stop mysql redis minio

# Check what's still running
running_services=$(docker-compose ps --services --filter "status=running" | wc -l)

if [ "$running_services" -eq 0 ]; then
    echo "✅ All services stopped"
else
    echo "ℹ️  Some services are still running:"
    docker-compose ps --filter "status=running"
fi

echo ""
echo "Infrastructure stopped:"
echo "  • MySQL: localhost:3306 (stopped)"
echo "  • Redis: localhost:6379 (stopped)"
echo "  • MinIO: localhost:9000/9001 (stopped)"
echo ""
echo "To restart infrastructure: ./start-infrastructure.sh"