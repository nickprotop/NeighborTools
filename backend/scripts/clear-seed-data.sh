#!/bin/bash

# Clear seed data script for NeighborTools
# This script removes all seeded test data to allow fresh seeding

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SQL_FILE="$SCRIPT_DIR/clear-seed-data.sql"

echo "🧹 Clearing NeighborTools seed data..."

# Check if MySQL container is running
if ! docker ps | grep -q "mysql"; then
    echo "❌ MySQL container is not running. Please start it first with:"
    echo "   cd $PROJECT_ROOT && docker-compose up -d mysql"
    exit 1
fi

# Check if SQL file exists
if [ ! -f "$SQL_FILE" ]; then
    echo "❌ SQL file not found: $SQL_FILE"
    exit 1
fi

# Get database connection info
DB_HOST="localhost"
DB_PORT="3306"
DB_NAME="toolssharing"
DB_USER="toolsuser"
DB_PASS="ToolsPassword123!"

echo "📋 Clearing seed data from database..."
echo "   Database: $DB_NAME"
echo "   Host: $DB_HOST:$DB_PORT"
echo "   User: $DB_USER"

# Execute the SQL script
if mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" < "$SQL_FILE"; then
    echo "✅ Seed data cleared successfully!"
    echo ""
    echo "📝 Cleared data includes:"
    echo "   • Test users (John Doe, Jane Smith)"
    echo "   • Sample tools (4 items)"
    echo "   • Sample rentals (3 items)"
    echo "   • Sample reviews (4 items)"
    echo "   • GDPR consent data"
    echo "   • Identity-related records"
    echo ""
    echo "💡 You can now run fresh seeding with:"
    echo "   cd $PROJECT_ROOT && dotnet run --project src/ToolsSharing.API --seed-only"
else
    echo "❌ Failed to clear seed data. Check database connection and permissions."
    exit 1
fi