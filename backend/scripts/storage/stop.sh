#!/bin/bash

# Stop storage services (PostgreSQL, Redis, MinIO)

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Calculate absolute paths before any directory changes
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../../docker" && pwd)"

echo "🛑 Stopping NeighborTools Storage Services"
echo "==========================================="

# Navigate to docker directory
cd "$DOCKER_DIR"

# Stop infrastructure services
echo "🔄 Stopping PostgreSQL, Redis, and MinIO..."
docker-compose --profile infrastructure stop

# Restore original directory
cd "$ORIGINAL_DIR"

echo "✅ Storage services stopped"
echo ""
echo "Note: Data is preserved. Use 'docker-compose down' to remove containers."
echo "To start again: ./storage/start.sh"