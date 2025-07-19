#!/bin/bash

# Simple test runner for NeighborTools
echo "🧪 Running NeighborTools Tests..."

cd "$(dirname "$0")/../tests/ToolsSharing.Tests"

echo "📦 Restoring packages..."
dotnet restore

echo "🔨 Building..."
dotnet build

echo "🧪 Running tests..."
dotnet test --verbosity normal

echo "✅ Tests completed!"