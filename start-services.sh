#!/bin/bash

# NeighborTools Development Environment Starter
# Starts both backend and frontend services

set -e  # Exit on any error

echo "🚀 Starting NeighborTools Development Environment"
echo "================================================"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! command_exists dotnet; then
    echo "❌ .NET SDK is required but not installed."
    echo "   Please install .NET 8 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command_exists docker; then
    echo "❌ Docker is required but not installed."
    echo "   Please install Docker from: https://docs.docker.com/get-docker/"
    exit 1
fi

echo "✅ Prerequisites check passed"
echo ""

# Start backend
echo "1️⃣ Starting Backend..."
echo "======================"
echo ""

if [ ! -d "backend" ]; then
    echo "❌ Backend directory not found. Are you in the NeighborTools root directory?"
    exit 1
fi

cd backend

# Check if backend is already set up
if [ ! -f ".backend-setup-complete" ]; then
    echo "🔧 First time setup detected. Running installation..."
    ./scripts/install.sh
    touch .backend-setup-complete
    echo ""
fi

echo "🔄 Starting backend services..."
./scripts/start-all.sh &
backend_pid=$!

cd ..

# Give backend time to start
echo "⏳ Waiting for backend to initialize..."
sleep 5

echo ""
echo "2️⃣ Starting Frontend..."
echo "======================"
echo ""

if [ ! -d "frontend" ]; then
    echo "❌ Frontend directory not found. Are you in the NeighborTools root directory?"
    exit 1
fi

cd frontend

echo "🔄 Starting frontend..."
echo "   This will start the Blazor WebAssembly application"
echo "   Press Ctrl+C to stop both services"
echo ""

# Start frontend (this will run in foreground)
dotnet run

# If we get here, frontend was stopped
echo ""
echo "🛑 Frontend stopped. Stopping backend..."
kill $backend_pid 2>/dev/null || true

echo ""
echo "✅ All services stopped"
echo ""
echo "Service URLs were:"
echo "  🌐 Frontend: http://localhost:5000"
echo "  🔧 Backend API: http://localhost:5002 "
echo "  📖 Swagger: http://localhost:5002/swagger"
echo ""
echo "To restart: ./start-services.sh"