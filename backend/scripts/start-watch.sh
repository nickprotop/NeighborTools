#!/bin/bash

# Complete development workflow: Storage + API with dotnet watch (hot reload)

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Navigate to scripts directory
SCRIPTS_DIR="$(dirname "$0")"
cd "$SCRIPTS_DIR"

echo "🚀 Starting NeighborTools Development Environment"
echo "=================================================="
echo "Mode: Storage services + Local API with hot reload (dotnet watch)"
echo ""

# Start storage services
echo "📦 Starting storage services..."
./storage/start.sh

echo ""
echo "🔥 Starting API with hot reload..."
./api/start-watch.sh

# Note: No need to restore directory here since start-watch.sh doesn't return