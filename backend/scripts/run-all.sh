#!/bin/bash

# Master script to run the entire Tools Sharing backend

echo "===================================================="
echo "Tools Sharing Backend - Complete Setup"
echo "===================================================="

# Function to handle errors
handle_error() {
    echo "Error occurred in: $1"
    echo "Stopping all services..."
    cd "$(dirname "$0")/../docker"
    docker-compose down
    exit 1
}

# Trap errors
trap 'handle_error "run-all.sh"' ERR

cd "$(dirname "$0")"

# Step 1: Start infrastructure
echo "Step 1: Starting infrastructure services..."
./start-infrastructure.sh || handle_error "start-infrastructure.sh"

# Step 2: Run migrations
echo ""
echo "Step 2: Running database migrations..."
./run-migrations.sh || handle_error "run-migrations.sh"

# Step 3: Seed database
echo ""
echo "Step 3: Seeding database..."
./seed-data.sh || handle_error "seed-data.sh"

echo ""
echo "===================================================="
echo "Backend setup completed successfully!"
echo "===================================================="
echo ""
echo "Services running:"
echo "- MySQL: localhost:3306"
echo "- Redis: localhost:6379"
echo ""
echo "To start the API server: ./scripts/start-api.sh"
echo "To stop all services: cd docker && docker-compose down"
echo ""
echo "Next steps:"
echo "1a. Start API locally: ./start-api.sh"
echo "1b. OR start API in Docker: ./start-docker-api.sh"
echo "1c. OR start complete Docker stack: ./start-docker-full.sh"
echo "2. Visit Swagger UI: http://0.0.0.0:5000/swagger (or http://localhost:5000/swagger)"
echo "3. Test the API endpoints"