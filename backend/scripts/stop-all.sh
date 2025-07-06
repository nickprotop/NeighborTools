#!/bin/bash

# Script to stop all services

echo "Stopping Tools Sharing Backend services..."

cd "$(dirname "$0")/../docker"

# Stop all Docker containers
docker-compose down

echo "All services stopped."
echo ""
echo "To remove all data (including database):"
echo "docker-compose down -v"