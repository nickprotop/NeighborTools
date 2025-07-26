#!/bin/bash

# Start API in Docker mode
# Used by start-all.sh when Docker mode is selected

set -e  # Exit on any error

echo "🐳 Starting API in Docker mode..."
echo "=================================="

# Navigate to docker directory
cd "$(dirname "$0")/../docker"

# Start the API service
echo "🚀 Starting API container..."
docker-compose --profile api up -d

echo ""
echo "✅ API started in Docker mode"
echo "🌐 API URL: http://localhost:5002"
echo "📖 Swagger: http://localhost:5002/swagger"
echo "📁 MinIO Console: http://localhost:9001"
echo ""
echo "Useful commands:"
echo "  📊 View logs: docker-compose logs -f api"
echo "  🔄 Restart: docker-compose restart api"
echo "  🛑 Stop: docker-compose stop api"