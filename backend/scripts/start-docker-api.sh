#!/bin/bash

# Script to start only the API in Docker (assumes infrastructure is already running)

echo "=========================================="
echo "Starting Tools Sharing API in Docker"
echo "=========================================="

cd "$(dirname "$0")/../docker"

# Function to handle errors
handle_error() {
    echo "Error occurred: $1"
    exit 1
}

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if infrastructure is running
echo "Checking infrastructure services..."
if ! docker-compose ps mysql | grep -q "Up"; then
    echo "MySQL is not running. Starting infrastructure first..."
    docker-compose up -d mysql redis
    echo "Waiting for MySQL to be ready..."
    sleep 15
fi

# Build and start API
echo "Building and starting API service..."
docker-compose up --build -d api

if [ $? -ne 0 ]; then
    handle_error "Failed to start API service"
fi

echo ""
echo "Waiting for API to be ready..."
sleep 10

# Check API status
echo ""
echo "API Status:"
echo "================================="
docker-compose ps api

echo ""
echo "Checking API health..."

# Wait for API to be ready
for i in {1..30}; do
    if curl -f http://localhost:5000/health >/dev/null 2>&1; then
        echo "✅ API is responding!"
        break
    elif [ $i -eq 30 ]; then
        echo "⚠️  API health check timeout, but service is running"
        break
    else
        echo "⏳ Waiting for API... ($i/30)"
        sleep 2
    fi
done

echo ""
echo "=========================================="
echo "🚀 API is now running in Docker!"
echo "=========================================="
echo ""
echo "Access URLs:"
echo "- API HTTP:  http://localhost:5000"
echo "- API HTTPS: https://localhost:5001"
echo "- Swagger:   http://localhost:5000/swagger"
echo "- Health:    http://localhost:5000/health"
echo ""
echo "Useful Commands:"
echo "- View API logs:    cd docker && docker-compose logs -f api"
echo "- Restart API:      cd docker && docker-compose restart api"
echo "- Stop API:         cd docker && docker-compose stop api"
echo "- Rebuild API:      cd docker && docker-compose up --build -d api"