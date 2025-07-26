#!/bin/bash

# Stop storage services (MySQL, Redis, MinIO)

set -e  # Exit on any error

echo "🛑 Stopping NeighborTools Storage Services"
echo "==========================================="

# Navigate to docker directory
DOCKER_DIR="$(dirname "$0")/../../docker"
cd "$DOCKER_DIR"

# Stop infrastructure services
echo "🔄 Stopping MySQL, Redis, and MinIO..."
docker-compose --profile infrastructure stop

echo "✅ Storage services stopped"
echo ""
echo "Note: Data is preserved. Use 'docker-compose down' to remove containers."
echo "To start again: ./storage/start.sh"