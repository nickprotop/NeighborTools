#!/bin/bash

# Script to start the complete application stack using Docker

echo "=========================================="
echo "Starting Tools Sharing - Complete Docker Stack"
echo "=========================================="

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

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    echo "Error: docker-compose.yml not found"
    exit 1
fi

echo "Building and starting all services..."
echo ""

# Build and start all services
docker-compose up --build -d

if [ $? -ne 0 ]; then
    handle_error "Failed to start Docker services"
fi

echo ""
echo "Waiting for services to be ready..."
sleep 10

# Check service status
echo ""
echo "Service Status:"
echo "================================="
docker-compose ps

echo ""
echo "Checking API health..."
sleep 5

# Wait for API to be ready
for i in {1..30}; do
    if curl -f http://localhost:5000/health >/dev/null 2>&1; then
        echo "✅ API is responding!"
        break
    elif [ $i -eq 30 ]; then
        echo "⚠️  API health check timeout, but services are running"
        break
    else
        echo "⏳ Waiting for API... ($i/30)"
        sleep 2
    fi
done

echo ""
echo "=========================================="
echo "🚀 All services are now running!"
echo "=========================================="
echo ""
echo "Access URLs:"
echo "- API HTTP:  http://localhost:5000"
echo "- API HTTPS: https://localhost:5001"
echo "- Swagger:   http://localhost:5000/swagger"
echo "- Health:    http://localhost:5000/health"
echo ""
echo "Database:"
echo "- MySQL:     localhost:3306"
echo "- Redis:     localhost:6379"
echo ""
echo "Container Status:"
docker-compose ps
echo ""
echo "Useful Commands:"
echo "- View logs:      cd docker && docker-compose logs -f"
echo "- Stop services:  cd docker && docker-compose down"
echo "- Restart API:    cd docker && docker-compose restart api"
echo "- View API logs:  cd docker && docker-compose logs -f api"