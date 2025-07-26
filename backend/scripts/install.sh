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
echo ""

# Database password configuration
echo "🔐 Database Configuration"
echo "========================"
echo "Configure passwords for the MySQL database setup."
echo "Press Enter to use default passwords (recommended for development)."
echo ""

# Function to read password with optional default
read_password() {
    local prompt="$1"
    local default="$2"
    local password
    
    while true; do
        echo -n "$prompt" >&2
        if [ -n "$default" ]; then
            echo -n " [default: $default]: " >&2
        else
            echo -n ": " >&2
        fi
        
        read -s password
        echo "" >&2  # New line after hidden input
        
        if [ -z "$password" ] && [ -n "$default" ]; then
            echo "$default"
            return
        elif [ -n "$password" ]; then
            echo "$password"
            return
        else
            echo "Please enter a password or press Enter for default." >&2
        fi
    done
}

# Get MySQL root password
MYSQL_ROOT_PASSWORD=$(read_password "MySQL root password" "RootPassword123!")

# Get MySQL user password
MYSQL_USER_PASSWORD=$(read_password "MySQL toolsuser password" "ToolsPassword123!")

# Get Redis password with production warning
echo ""
echo "⚠️  Redis Security Configuration"
echo "================================"
echo "🚨 IMPORTANT: This will enable Redis password authentication."
echo "   - New installations: Redis will require password"
echo "   - Existing production: This WILL break existing passwordless connections"
echo "   - All applications must use: localhost:6379,password=YourPassword"
echo ""
echo "Choose Redis configuration:"
echo "1) Enable Redis password (recommended for security)"
echo "2) Skip Redis password (keep existing behavior - NOT SECURE)"
echo ""
read -p "Enter choice [1-2] (default: 1): " redis_choice
redis_choice=${redis_choice:-1}

if [ "$redis_choice" = "1" ]; then
    REDIS_PASSWORD=$(read_password "Redis password" "RedisPassword123!")
    ENABLE_REDIS_PASSWORD=true
else
    echo "⚠️  WARNING: Redis will run WITHOUT password authentication"
    echo "   This is NOT recommended for production or network-accessible Redis"
    REDIS_PASSWORD=""
    ENABLE_REDIS_PASSWORD=false
fi

echo ""
echo "📁 MinIO File Storage Configuration"
echo "=================================="
echo "Configure MinIO object storage for file uploads and user content."
echo "Press Enter to use default values (recommended for development)."
echo ""

# Get MinIO configuration
MINIO_ENDPOINT=$(read_password "MinIO endpoint (e.g., localhost:9000)" "localhost:9000")
MINIO_ROOT_USER=$(read_password "MinIO root username" "minioadmin")
MINIO_ROOT_PASSWORD=$(read_password "MinIO root password" "MinIOPassword123!")

echo ""
echo "🌐 Blazor WASM App Configuration"
echo "==============================="
echo "Configure the Blazor WebAssembly app URL for development."
echo ""

# Function to read text input with default
read_input() {
    local prompt="$1"
    local default="$2"
    local input
    
    echo -n "$prompt" >&2
    if [ -n "$default" ]; then
        echo -n " [default: $default]: " >&2
    else
        echo -n ": " >&2
    fi
    
    read input
    
    if [ -z "$input" ] && [ -n "$default" ]; then
        echo "$default"
    else
        echo "$input"
    fi
}

FRONTEND_BASE_URL=$(read_input "Blazor WASM app URL" "http://localhost:5000")

echo ""
echo "✅ Configuration complete"
echo "================================================"
echo "Review your configuration:"
echo "   MySQL root password: $(echo "$MYSQL_ROOT_PASSWORD" | sed 's/./*/g')"
echo "   MySQL user password: $(echo "$MYSQL_USER_PASSWORD" | sed 's/./*/g')"
if [ "$ENABLE_REDIS_PASSWORD" = "true" ]; then
    echo "   Redis password: $(echo "$REDIS_PASSWORD" | sed 's/./*/g')"
else
    echo "   Redis password: DISABLED (no authentication)"
fi
echo "   Blazor WASM app URL: $FRONTEND_BASE_URL"
echo "================================================"
echo ""
echo -n "Proceed with installation? [Y/n]: " >&2
read -r confirm
echo ""

if [[ "$confirm" =~ ^[Nn]$ ]]; then
    echo "❌ Installation cancelled by user."
    exit 0
fi

echo "🚀 Starting installation..."

# Create .env file for docker-compose
DOCKER_DIR="$(dirname "$0")/../docker"
ENV_FILE="$DOCKER_DIR/.env"

echo "📝 Creating .env file for docker-compose..."
cat > "$ENV_FILE" << EOF
# Docker Compose Environment Variables
# Generated by install script on $(date)

# MySQL Configuration
MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD
MYSQL_USER_PASSWORD=$MYSQL_USER_PASSWORD

# Redis Configuration
REDIS_PASSWORD=$REDIS_PASSWORD
ENABLE_REDIS_PASSWORD=$ENABLE_REDIS_PASSWORD

# MinIO Configuration
MINIO_ROOT_USER=$MINIO_ROOT_USER
MINIO_ROOT_PASSWORD=$MINIO_ROOT_PASSWORD

# Note: This file is automatically read by docker-compose
EOF

echo "✅ Created .env file: $ENV_FILE"

# Navigate to the docker directory
cd "$DOCKER_DIR"

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
    if [ "$ENABLE_REDIS_PASSWORD" = "true" ]; then
        if docker-compose exec -T redis redis-cli -a "$REDIS_PASSWORD" ping | grep -q "PONG"; then
            echo "✅ Redis is ready with authentication"
            break
        fi
    else
        if docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
            echo "✅ Redis is ready (no authentication)"
            break
        fi
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

# Update backend configuration with database password
echo "📝 Updating backend configuration..."
cd src/ToolsSharing.API

# Check if config.json exists, if not create it from sample
if [ ! -f "config.json" ]; then
    if [ -f "config.sample.json" ]; then
        cp config.sample.json config.json
        echo "✅ Created config.json from sample"
    else
        echo "❌ config.sample.json not found. Cannot create config.json"
        exit 1
    fi
fi

# Update the database connection string, Redis connection, and Blazor WASM app URL in config.json
CONNECTION_STRING="server=localhost;port=3306;database=toolssharing;uid=toolsuser;pwd=${MYSQL_USER_PASSWORD}"
if [ "$ENABLE_REDIS_PASSWORD" = "true" ]; then
    REDIS_CONNECTION_STRING="localhost:6379,password=${REDIS_PASSWORD}"
else
    REDIS_CONNECTION_STRING="localhost:6379"
fi
if command -v jq &> /dev/null; then
    # Use jq if available for proper JSON manipulation
    tmp=$(mktemp)
    jq --arg conn "$CONNECTION_STRING" --arg redis "$REDIS_CONNECTION_STRING" --arg frontend "$FRONTEND_BASE_URL" \
       --arg minioEndpoint "$MINIO_ENDPOINT" --arg minioKey "$MINIO_ROOT_USER" --arg minioSecret "$MINIO_ROOT_PASSWORD" \
       '
       .ConnectionStrings.DefaultConnection = $conn |
       .ConnectionStrings.Redis = $redis |
       .Frontend.BaseUrl = $frontend |
       if .Payment then .Payment.FrontendBaseUrl = $frontend else . + {"Payment": {"FrontendBaseUrl": $frontend}} end |
       . + {"MinIO": {
         "Endpoint": $minioEndpoint,
         "AccessKey": $minioKey, 
         "SecretKey": $minioSecret,
         "Secure": false,
         "BucketName": "toolssharing-files"
       }}
       ' \
       config.json > "$tmp" && mv "$tmp" config.json
    echo "✅ Updated database connection, Blazor WASM app URL, and MinIO configuration in config.json (using jq)"
else
    # Fallback to sed - simpler approach using Python for JSON manipulation
    if command -v python3 &> /dev/null; then
        # Use Python for proper JSON manipulation if jq not available
        python3 << EOF
import json
import sys

try:
    with open('config.json', 'r') as f:
        config = json.load(f)
    
    # Update configuration
    config['ConnectionStrings']['DefaultConnection'] = '$CONNECTION_STRING'
    config['ConnectionStrings']['Redis'] = '$REDIS_CONNECTION_STRING'
    config['Frontend']['BaseUrl'] = '$FRONTEND_BASE_URL'
    
    # Update or create Payment section
    if 'Payment' not in config:
        config['Payment'] = {}
    config['Payment']['FrontendBaseUrl'] = '$FRONTEND_BASE_URL'
    
    # Always create/overwrite MinIO section
    config['MinIO'] = {
        'Endpoint': '$MINIO_ENDPOINT',
        'AccessKey': '$MINIO_ROOT_USER', 
        'SecretKey': '$MINIO_ROOT_PASSWORD',
        'Secure': False,
        'BucketName': 'toolssharing-files'
    }
    
    # Write back to file
    with open('config.json', 'w') as f:
        json.dump(config, f, indent=2)
        
    print('✅ Updated configuration successfully')
except Exception as e:
    print(f'❌ Error updating config.json: {e}')
    sys.exit(1)
EOF
        echo "✅ Updated database connection, Blazor WASM app URL, and MinIO configuration in config.json (using Python)"
    else
        # Last resort: basic sed replacements (limited functionality)
        echo "⚠️  Neither jq nor Python3 available - using basic sed (limited functionality)"
        sed -i "s|\"DefaultConnection\": \".*\"|\"DefaultConnection\": \"$CONNECTION_STRING\"|g" config.json
        sed -i "s|\"Redis\": \".*\"|\"Redis\": \"$REDIS_CONNECTION_STRING\"|g" config.json
        sed -i "s|\"BaseUrl\": \".*\"|\"BaseUrl\": \"$FRONTEND_BASE_URL\"|g" config.json
        echo "⚠️  MinIO configuration may need manual setup in config.json"
        echo "   Add this section to config.json:"
        echo '   "MinIO": {'
        echo '     "Endpoint": "'$MINIO_ENDPOINT'",'
        echo '     "AccessKey": "'$MINIO_ROOT_USER'",'
        echo '     "SecretKey": "'$MINIO_ROOT_PASSWORD'",'
        echo '     "Secure": false,'
        echo '     "BucketName": "toolssharing-files"'
        echo '   }'
    fi
fi

cd ../..

# Apply database migrations (essential system data included)
echo "🗄️ Running database migrations with essential system data..."
dotnet run --project src/ToolsSharing.API --seed-only

echo ""
echo "🎉 Installation completed successfully!"
echo "============================================="
echo "Database Configuration:"
echo "  • MySQL Root Password: $(echo "$MYSQL_ROOT_PASSWORD" | sed 's/./*/g')"
echo "  • MySQL User Password: $(echo "$MYSQL_USER_PASSWORD" | sed 's/./*/g')"
if [ "$ENABLE_REDIS_PASSWORD" = "true" ]; then
    echo "  • Redis Password: $(echo "$REDIS_PASSWORD" | sed 's/./*/g')"
else
    echo "  • Redis Password: DISABLED (no authentication)"
fi
echo ""
echo "File Storage Configuration:"
echo "  • MinIO Console: http://localhost:9001 (user: $MINIO_ROOT_USER)"
echo "  • MinIO Password: $(echo "$MINIO_ROOT_PASSWORD" | sed 's/./*/g')"
echo ""
echo "Next steps - Choose your development workflow:"
echo "  • './start-watch.sh' - Active development (storage + API with hot reload)"
echo "  • './start-dev.sh' - Stable development (storage + API for debugging)"
echo "  • './start-production.sh' - Production testing (storage + Docker API)"
echo ""
echo "Or use granular control:"
echo "  • './storage/start.sh' then './api/start-local.sh' (dotnet run)"
echo "  • './storage/start.sh' then './api/start-watch.sh' (hot reload)"
echo "  • './storage/start.sh' then './api/start-docker.sh' (Docker API)"
echo ""
echo "Access points:"
echo "  • Access Swagger UI at: http://localhost:5002/swagger"
echo "  • Blazor WASM app will be available at: $FRONTEND_BASE_URL"
echo "  • MySQL: localhost:3306 (user: toolsuser, password: [configured above])"
if [ "$ENABLE_REDIS_PASSWORD" = "true" ]; then
    echo "  • Redis: localhost:6379 (password: [configured above])"
else
    echo "  • Redis: localhost:6379 (no authentication)"
fi
echo ""
echo "Admin Access:"
echo "  • Essential admin user created: admin@neighbortools.com / Admin123!"
echo "  • Essential data (roles, tool categories) installed automatically"
echo "  • Use Admin Panel → Sample Data Management to add/remove test data"
echo "  • Optional: Add sample users (john.doe@email.com, jane.smith@email.com) via admin panel"
echo ""
echo "Configuration Files Updated:"
echo "  • Docker Compose: Uses environment variables with fallback to defaults"
echo "  • Backend config.json: Updated with database password and Blazor WASM app URL"
echo ""
echo "Happy coding! 🚀"