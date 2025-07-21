#!/bin/bash

# NeighborTools Development Environment Setup Script
# This script orchestrates the complete development setup process

set -e  # Exit on any error

echo "🚀 NeighborTools Development Environment Setup"
echo "=============================================="
echo ""
echo "This script will guide you through setting up your development environment."
echo "It will:"
echo "  1. Run backend installation (database setup, migrations)"
echo "  2. Start infrastructure services (MySQL, Redis)"
echo "  3. Configure frontend settings"
echo "  4. Provide instructions for running the application"
echo ""
echo -n "Continue? [Y/n]: "
read -r confirm
echo ""

if [[ "$confirm" =~ ^[Nn]$ ]]; then
    echo "❌ Setup cancelled."
    exit 0
fi

# Step 1: Run backend installation
echo "📦 Step 1: Backend Installation"
echo "==============================="
echo "Running backend installation script..."
echo ""

cd backend/scripts
./install.sh
cd ../..

echo ""
echo "✅ Backend installation complete!"
echo ""

# Step 2: Prompt to start infrastructure
echo "🏗️  Step 2: Infrastructure Services"
echo "=================================="
echo "The infrastructure services (MySQL and Redis) need to be running."
echo ""
echo "Would you like to start them now? [Y/n]: "
read -r start_infra

if [[ ! "$start_infra" =~ ^[Nn]$ ]]; then
    echo "Starting infrastructure services..."
    cd backend/scripts
    ./start-infrastructure.sh
    cd ../..
    
    # Wait a moment for services to be ready
    echo "⏳ Waiting for services to be ready..."
    sleep 5
    
    echo "✅ Infrastructure services started!"
else
    echo "⚠️  Remember to start infrastructure manually before running the application:"
    echo "   cd backend/scripts && ./start-infrastructure.sh"
fi

echo ""

# Step 3: Configure frontend
echo "🎨 Step 3: Frontend Configuration"
echo "================================="
echo "Now let's configure the frontend application..."
echo ""

cd frontend
./configure.sh
cd ..

echo ""
echo "✅ Frontend configuration complete!"
echo ""

# Step 4: Display configuration summary
echo ""
echo "🎉 Setup Complete!"
echo "=================="
echo ""

# Use the show-config script to display all configuration
./show-config.sh