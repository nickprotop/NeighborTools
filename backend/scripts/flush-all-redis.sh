#!/bin/bash

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Navigate to scripts directory
SCRIPTS_DIR="$(dirname "$0")"
cd "$SCRIPTS_DIR"

# Load environment variables from .env file
if [ -f "../docker/.env" ]; then
    export $(grep -v '^#' ../docker/.env | xargs)
else
    echo "Error: .env file not found at ../docker/.env"
    exit 1
fi

echo "This will delete ALL data from Redis cache."
echo "Are you sure you want to continue? (y/N)"
read -r response

if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "Flushing all Redis data..."
    
    if [[ "$ENABLE_REDIS_PASSWORD" == "true" ]]; then
        docker exec -it tools-sharing-redis redis-cli -a "$REDIS_PASSWORD" FLUSHALL
    else
        docker exec -it tools-sharing-redis redis-cli FLUSHALL
    fi
    
    echo "Redis cache cleared successfully."
else
    echo "Operation cancelled."
    cd "$ORIGINAL_DIR"
    exit 1
fi

# Return to original directory
cd "$ORIGINAL_DIR"