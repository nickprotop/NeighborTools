#!/bin/bash

# Script to start infrastructure services (MySQL, Redis)

echo "Starting infrastructure services..."

cd "$(dirname "$0")/../docker"

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    echo "Error: docker-compose.yml not found"
    exit 1
fi

# Start only infrastructure services (MySQL, Redis)
echo "Starting MySQL and Redis..."
docker-compose up -d mysql redis

# Wait for MySQL to be ready
echo "Waiting for MySQL to be ready..."
for i in {1..30}; do
    if docker-compose exec mysql mysqladmin ping -h"localhost" --silent; then
        echo "MySQL is ready!"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "Error: MySQL failed to start within 30 seconds"
        exit 1
    fi
    echo "Waiting for MySQL... ($i/30)"
    sleep 2
done

# Wait for Redis to be ready
echo "Waiting for Redis to be ready..."
for i in {1..10}; do
    if docker-compose exec redis redis-cli ping | grep -q PONG; then
        echo "Redis is ready!"
        break
    fi
    if [ $i -eq 10 ]; then
        echo "Error: Redis failed to start within 20 seconds"
        exit 1
    fi
    echo "Waiting for Redis... ($i/10)"
    sleep 2
done

echo "Infrastructure services started successfully!"
echo ""
echo "Services running:"
echo "- MySQL: localhost:3306"
echo "- Redis: localhost:6379"
echo ""
echo "To stop infrastructure: docker-compose down"