#!/bin/bash

# Complete production-like workflow: Storage + API in Docker

set -e  # Exit on any error

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