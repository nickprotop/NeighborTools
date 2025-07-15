#!/bin/bash

# Show current database configuration
echo "🔍 NeighborTools Database Configuration"
echo "======================================"

CONFIG_FILE="$(dirname "$0")/../src/ToolsSharing.API/config.json"

if [ -f "$CONFIG_FILE" ]; then
    echo "📝 Reading from: $CONFIG_FILE"
    
    if command -v jq &> /dev/null; then
        # Use jq for pretty output
        DB_CONNECTION=$(jq -r '.ConnectionStrings.DefaultConnection' "$CONFIG_FILE")
        echo "🔗 Database Connection:"
        echo "   $DB_CONNECTION"
        
        # Extract password and mask it
        if [[ "$DB_CONNECTION" =~ pwd=([^;]*) ]]; then
            PASSWORD="${BASH_REMATCH[1]}"
            MASKED_PASSWORD=$(echo "$PASSWORD" | sed 's/./*/g')
            echo "🔐 Database Password: $MASKED_PASSWORD"
        fi
    else
        # Fallback without jq
        echo "🔗 Database Connection:"
        grep -o '"DefaultConnection":[^"]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*"DefaultConnection":[^"]*"\([^"]*\)".*/\1/'
    fi
else
    echo "❌ config.json not found at: $CONFIG_FILE"
    echo "💡 Run the install script first: ./install.sh"
fi

echo ""
echo "📊 Docker Configuration (.env file):"
DOCKER_DIR="$(dirname "$0")/../docker"
ENV_FILE="$DOCKER_DIR/.env"

if [ -f "$ENV_FILE" ]; then
    echo "   📝 .env file: $ENV_FILE"
    echo "   MYSQL_ROOT_PASSWORD: $(grep "MYSQL_ROOT_PASSWORD=" "$ENV_FILE" | cut -d'=' -f2 | sed 's/./*/g')"
    echo "   MYSQL_USER_PASSWORD: $(grep "MYSQL_USER_PASSWORD=" "$ENV_FILE" | cut -d'=' -f2 | sed 's/./*/g')"
else
    echo "   ❌ No .env file found"
    echo "   💡 Run ./install.sh to create configuration"
fi

echo ""
echo "🐳 Docker Compose Status:"
cd "$(dirname "$0")/../docker"
if docker-compose ps --services --filter "status=running" | grep -q "mysql"; then
    echo "   MySQL: ✅ Running"
else
    echo "   MySQL: ❌ Not running"
fi

if docker-compose ps --services --filter "status=running" | grep -q "redis"; then
    echo "   Redis: ✅ Running"
else
    echo "   Redis: ❌ Not running"
fi