#!/bin/bash

# Stop all NeighborTools services
# Complete shutdown of development environment

set -e  # Exit on any error

echo "🛑 Stopping All NeighborTools Services"
echo "======================================"

# Navigate to docker directory
cd "$(dirname "$0")/../docker"

# Stop all services
echo "🔄 Stopping all containers..."
docker-compose down

# Remove any orphaned containers
echo "🧹 Cleaning up orphaned containers..."
docker-compose down --remove-orphans

echo ""
echo "✅ All services stopped and containers removed"
echo ""
echo "Services stopped:"
echo "  • API: localhost:5002 (stopped)"
echo "  • MySQL: localhost:3306 (stopped)"  
echo "  • Redis: localhost:6379 (stopped)"
echo ""
echo "Data volumes preserved (database data is safe)"
echo ""
echo "To restart everything: ./start-all.sh"