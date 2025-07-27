#!/bin/bash

# Complete development workflow: Storage + API with dotnet run (stable development)

set -e  # Exit on any error

# Remember current directory
ORIGINAL_DIR="$(pwd)"

# Navigate to scripts directory
SCRIPTS_DIR="$(dirname "$0")"
cd "$SCRIPTS_DIR"

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

# Note: No need to restore directory here since start-local.sh doesn't return