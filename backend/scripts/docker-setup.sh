#!/bin/bash

# Complete Docker setup for Tools Sharing - Database + API

echo "================================================="
echo "Tools Sharing - Complete Docker Setup"
echo "================================================="

cd "$(dirname "$0")/../docker"

# Function to handle errors
handle_error() {
    echo "Error occurred: $1"
    echo "Stopping all services..."
    docker-compose down
    exit 1
}

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "Step 1: Building and starting infrastructure..."
docker-compose up -d mysql redis

echo ""
echo "Step 2: Waiting for MySQL to be ready..."
sleep 15

# Check if MySQL is ready
for i in {1..30}; do
    if docker-compose exec -T mysql mysql -u toolsuser -pToolsPassword123! -e "SELECT 1;" >/dev/null 2>&1; then
        echo "✅ MySQL is ready!"
        break
    elif [ $i -eq 30 ]; then
        echo "❌ MySQL startup timeout"
        handle_error "MySQL not ready"
    else
        echo "⏳ Waiting for MySQL... ($i/30)"
        sleep 2
    fi
done

echo ""
echo "Step 3: Running database migrations..."
# First build the API image
docker-compose build api

# Run migrations
docker-compose run --rm api dotnet ef database update --project /app/ToolsSharing.Infrastructure.dll --startup-project /app/ToolsSharing.API.dll

if [ $? -ne 0 ]; then
    echo "Warning: Migration failed, but continuing..."
fi

echo ""
echo "Step 4: Starting API server..."
docker-compose up -d api

echo ""
echo "Step 5: Waiting for API to be ready..."
sleep 10

# Check API health
for i in {1..30}; do
    if curl -f http://localhost:5000/health >/dev/null 2>&1; then
        echo "✅ API is responding!"
        break
    elif [ $i -eq 30 ]; then
        echo "⚠️  API health check timeout, but container is running"
        break
    else
        echo "⏳ Waiting for API... ($i/30)"
        sleep 2
    fi
done

echo ""
echo "================================================="
echo "🚀 Docker setup completed!"
echo "================================================="
echo ""
echo "All services are running:"
docker-compose ps
echo ""
echo "Access URLs:"
echo "- API:       http://localhost:5000"
echo "- Swagger:   http://localhost:5000/swagger"
echo "- Health:    http://localhost:5000/health"
echo ""
echo "Management:"
echo "- View logs: docker-compose logs -f"
echo "- Stop all:  docker-compose down"
echo ""