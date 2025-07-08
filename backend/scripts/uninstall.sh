#!/bin/bash

# Complete uninstallation of NeighborTools
# WARNING: This will delete all data including database

set -e  # Exit on any error

echo "⚠️  NeighborTools Complete Uninstallation"
echo "========================================="
echo ""
echo "🚨 WARNING: This will permanently delete:"
echo "   • All Docker containers and images"
echo "   • All database data (tools, users, rentals)"
echo "   • All cached data in Redis"
echo "   • Docker volumes and networks"
echo ""

# Confirmation prompt
read -p "Are you sure you want to proceed? Type 'YES' to continue: " confirmation

if [ "$confirmation" != "YES" ]; then
    echo "❌ Uninstallation cancelled"
    exit 0
fi

echo ""
echo "🧹 Starting complete cleanup..."

# Navigate to docker directory
cd "$(dirname "$0")/../docker"

# Stop and remove all containers, networks, and volumes
echo "🔄 Stopping and removing containers..."
docker-compose down --volumes --remove-orphans

# Remove Docker images built for this project
echo "🔄 Removing Docker images..."
if docker images | grep -q "tools-sharing"; then
    docker images | grep "tools-sharing" | awk '{print $3}' | xargs docker rmi -f || true
fi

# Remove any dangling images and volumes
echo "🔄 Cleaning up dangling resources..."
docker system prune -f --volumes || true

# Navigate back to backend root
cd ..

# Remove development preferences
echo "🔄 Cleaning up development files..."
[ -f ".dev-mode" ] && rm .dev-mode

echo ""
echo "✅ Uninstallation completed successfully!"
echo "======================================="
echo ""
echo "All NeighborTools components have been removed:"
echo "  • Docker containers: removed"
echo "  • Docker images: removed"
echo "  • Database data: deleted"
echo "  • Redis cache: deleted"
echo "  • Docker volumes: deleted"
echo ""
echo "To reinstall NeighborTools:"
echo "  • Run: ./install.sh"
echo ""
echo "Source code remains intact in the repository."