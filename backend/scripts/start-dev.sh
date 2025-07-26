#!/bin/bash

# Complete development workflow: Storage + API with dotnet run (stable development)

set -e  # Exit on any error

echo "🚀 Starting NeighborTools Development Environment"
echo "=================================================="
echo "Mode: Storage services + Local API (dotnet run)"
echo ""

# Start storage services
echo "📦 Starting storage services..."
./storage/start.sh

echo ""
echo "💻 Starting API locally..."
./api/start-local.sh