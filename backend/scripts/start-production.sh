#!/bin/bash

# Complete production-like workflow: Storage + API in Docker

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Navigate to scripts directory
SCRIPTS_DIR="$(dirname "$0")"
cd "$SCRIPTS_DIR"

echo "🚀 Starting NeighborTools Production Environment"
echo "================================================="
echo "Mode: Storage services + Docker API (production-like testing)"
echo ""

# Start storage services
echo "📦 Starting storage services..."
./storage/start.sh

echo ""
echo "🐳 Starting API in Docker..."
./api/start-docker.sh

# Restore original directory
cd "$ORIGINAL_DIR"