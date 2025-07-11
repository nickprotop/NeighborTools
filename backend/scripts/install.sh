#!/bin/bash

# NeighborTools Complete Installation Script
# Run this once for initial project setup

set -e  # Exit on any error

echo "🚀 Installing NeighborTools - Complete Setup"
echo "============================================="

# Check if Docker and Docker Compose are installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 9 SDK first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Navigate to the docker directory
cd "$(dirname "$0")/../docker"

# Stop any existing containers
echo "🧹 Cleaning up existing containers..."
docker-compose down --remove-orphans

# Install infrastructure (MySQL, Redis)
echo "📦 Setting up infrastructure (MySQL, Redis)..."
docker-compose --profile infrastructure up -d

# Wait for MySQL to be ready
echo "⏳ Waiting for MySQL to be ready..."
for i in {1..30}; do
    if docker-compose exec -T mysql mysqladmin ping -h localhost --silent; then
        echo "✅ MySQL is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "❌ MySQL failed to start within 30 seconds"
        exit 1
    fi
    echo "   Waiting... ($i/30)"
    sleep 1
done

# Wait for Redis to be ready
echo "⏳ Waiting for Redis to be ready..."
for i in {1..10}; do
    if docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
        echo "✅ Redis is ready"
        break
    fi
    if [ $i -eq 10 ]; then
        echo "❌ Redis failed to start within 10 seconds"
        exit 1
    fi
    echo "   Waiting... ($i/10)"
    sleep 1
done

# Navigate back to backend root
cd ..

# Install .NET dependencies
echo "📦 Installing .NET dependencies..."
dotnet restore

# Seed initial data (migrations will run automatically)
echo "🌱 Running database migrations and seeding initial data..."
dotnet run --project src/ToolsSharing.API --seed-only

echo ""
echo "🎉 Installation completed successfully!"
echo "============================================="
echo "Next steps:"
echo "  • Run './start-all.sh' to start development environment"
echo "  • Or run './start-infrastructure.sh' + 'dotnet run' for API debugging"
echo "  • Access Swagger UI at: http://localhost:5002/swagger"
echo "  • MySQL: localhost:3306 (user: toolsuser, password: ToolsPassword123!)"
echo "  • Redis: localhost:6379"
echo ""
echo "Happy coding! 🚀"