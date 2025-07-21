#!/bin/bash

# NeighborTools Configuration Display Script
# Shows current configuration from all config files

echo "🔍 NeighborTools Configuration Summary"
echo "======================================"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" &> /dev/null
}

# Frontend Configuration
echo "🎨 Frontend Configuration"
echo "------------------------"
FRONTEND_CONFIG="frontend/wwwroot/config.json"
if [ -f "$FRONTEND_CONFIG" ]; then
    if command_exists jq; then
        FRONTEND_URL=$(jq -r '.Site.HomePageUrl' "$FRONTEND_CONFIG" 2>/dev/null || echo "Not configured")
        API_URL=$(jq -r '.ApiSettings.BaseUrl' "$FRONTEND_CONFIG" 2>/dev/null || echo "Not configured")
        ENVIRONMENT=$(jq -r '.Environment' "$FRONTEND_CONFIG" 2>/dev/null || echo "Not configured")
        ANALYTICS=$(jq -r '.Features.EnableAnalytics' "$FRONTEND_CONFIG" 2>/dev/null || echo "Not configured")
    else
        # Fallback to grep if jq not available
        API_URL=$(grep -o '"BaseUrl"[[:space:]]*:[[:space:]]*"[^"]*"' "$FRONTEND_CONFIG" 2>/dev/null | sed 's/.*"BaseUrl"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo "Not configured")
        FRONTEND_URL=$(grep -o '"HomePageUrl"[[:space:]]*:[[:space:]]*"[^"]*"' "$FRONTEND_CONFIG" 2>/dev/null | sed 's/.*"HomePageUrl"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo "Not configured")
        ENVIRONMENT=$(grep -o '"Environment"[[:space:]]*:[[:space:]]*"[^"]*"' "$FRONTEND_CONFIG" 2>/dev/null | sed 's/.*"Environment"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo "Not configured")
        ANALYTICS=$(grep -o '"EnableAnalytics"[[:space:]]*:[[:space:]]*[^,}]*' "$FRONTEND_CONFIG" 2>/dev/null | sed 's/.*"EnableAnalytics"[[:space:]]*:[[:space:]]*\([^,}]*\).*/\1/' || echo "Not configured")
    fi
    
    echo "   Frontend URL: $FRONTEND_URL"
    echo "   API URL: $API_URL"
    echo "   Environment: $ENVIRONMENT"
    echo "   Analytics Enabled: $ANALYTICS"
    echo "   Config File: $FRONTEND_CONFIG"
else
    echo "   ❌ Frontend not configured"
    echo "   💡 Run: cd frontend && ./configure.sh"
fi

echo ""

# Backend Configuration
echo "🔧 Backend Configuration"
echo "-----------------------"
BACKEND_CONFIG="backend/src/ToolsSharing.API/config.json"
if [ -f "$BACKEND_CONFIG" ]; then
    if command_exists jq; then
        # Database connection
        DB_CONNECTION=$(jq -r '.ConnectionStrings.DefaultConnection' "$BACKEND_CONFIG" 2>/dev/null || echo "Not configured")
        # Frontend URL configured in backend
        BACKEND_FRONTEND_URL=$(jq -r '.Frontend.BaseUrl' "$BACKEND_CONFIG" 2>/dev/null || echo "Not configured")
        # PayPal configuration status
        PAYPAL_ENV=$(jq -r '.Payment.PayPal.Environment' "$BACKEND_CONFIG" 2>/dev/null || echo "Not configured")
        # Email configuration status
        EMAIL_FROM=$(jq -r '.Email.From' "$BACKEND_CONFIG" 2>/dev/null || echo "Not configured")
    else
        # Fallback to grep
        DB_CONNECTION=$(grep -o '"DefaultConnection"[[:space:]]*:[[:space:]]*"[^"]*"' "$BACKEND_CONFIG" 2>/dev/null | sed 's/.*"DefaultConnection"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo "Not configured")
        BACKEND_FRONTEND_URL=$(grep -o '"BaseUrl"[[:space:]]*:[[:space:]]*"[^"]*"' "$BACKEND_CONFIG" 2>/dev/null | tail -1 | sed 's/.*"BaseUrl"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo "Not configured")
        PAYPAL_ENV="Check manually"
        EMAIL_FROM="Check manually"
    fi
    
    # Extract and mask database password
    if [[ "$DB_CONNECTION" =~ pwd=([^;]*) ]]; then
        PASSWORD="${BASH_REMATCH[1]}"
        MASKED_PASSWORD=$(echo "$PASSWORD" | sed 's/./*/g')
        MASKED_CONNECTION=$(echo "$DB_CONNECTION" | sed "s/pwd=$PASSWORD/pwd=$MASKED_PASSWORD/")
    else
        MASKED_CONNECTION="$DB_CONNECTION"
    fi
    
    echo "   API URL: http://localhost:5002"
    echo "   Swagger UI: http://localhost:5002/swagger"
    echo "   Database: $MASKED_CONNECTION"
    echo "   Frontend URL: $BACKEND_FRONTEND_URL"
    echo "   PayPal Environment: $PAYPAL_ENV"
    echo "   Email From: $EMAIL_FROM"
    echo "   Config File: $BACKEND_CONFIG"
else
    echo "   ❌ Backend not configured"
    echo "   💡 Run: cd backend/scripts && ./install.sh"
fi

echo ""

# Infrastructure Status
echo "🐳 Infrastructure Status"
echo "-----------------------"
cd backend/docker 2>/dev/null || cd docker 2>/dev/null || true

if command_exists docker-compose; then
    MYSQL_STATUS="❌ Not running"
    REDIS_STATUS="❌ Not running"
    
    if docker-compose ps --services --filter "status=running" 2>/dev/null | grep -q "mysql"; then
        MYSQL_STATUS="✅ Running"
    fi
    
    if docker-compose ps --services --filter "status=running" 2>/dev/null | grep -q "redis"; then
        REDIS_STATUS="✅ Running"
    fi
    
    echo "   MySQL: $MYSQL_STATUS (localhost:3306)"
    echo "   Redis: $REDIS_STATUS (localhost:6379)"
else
    echo "   ❌ Docker Compose not installed"
fi

cd - > /dev/null 2>&1

# Docker environment file (check from root directory)
ENV_FILE="backend/docker/.env"
if [ -f "$ENV_FILE" ]; then
    echo "   Docker .env: ✅ Configured"
else
    echo "   Docker .env: ❌ Not found"
fi

echo ""

# Default Accounts
echo "👤 System Accounts"
echo "------------------"
echo "   Admin: admin@neighbortools.com / Admin123! (created by migrations)"
echo ""
echo "   Sample Data Users (only if installed via Admin Panel):"
echo "   - john.doe@email.com / Password123!"
echo "   - jane.smith@email.com / Password123!"

echo ""

# Quick Commands
echo "⚡ Quick Commands"
echo "-----------------"
echo "   Start Everything:    ./start-services.sh"
echo "   Start Backend Only:  cd backend && dotnet run --project src/ToolsSharing.API"
echo "   Start Frontend Only: cd frontend && dotnet run"
echo "   Stop Infrastructure: ./backend/scripts/stop-all.sh"
echo "   View Logs:          docker-compose -f backend/docker/docker-compose.yml logs -f"

echo ""
echo "======================================"
echo "✅ Configuration check complete"