#!/bin/bash

# Production build script for NeighborTools
# Usage: ./build-for-production.sh [OPTIONS]
# 
# Options:
#   --api-url URL          API base URL (default: https://api.neighbortools.com)
#   --environment ENV      Environment name (default: Production)
#   --output-dir DIR       Output directory (default: ./publish)
#   --help                 Show this help message

# Set defaults
API_URL="https://api.neighbortools.com"
ENVIRONMENT="Production"
OUTPUT_DIR="./publish"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --api-url)
            API_URL="$2"
            shift 2
            ;;
        --environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --help|-h)
            echo "Production build script for NeighborTools"
            echo ""
            echo "Usage: ./build-for-production.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --api-url URL          API base URL (default: https://api.neighbortools.com)"
            echo "  --environment ENV      Environment name (default: Production)"
            echo "  --output-dir DIR       Output directory (default: ./publish)"
            echo "  --help, -h             Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./build-for-production.sh"
            echo "  ./build-for-production.sh --api-url \"https://api.yourapp.com\""
            echo "  ./build-for-production.sh --environment \"Staging\" --output-dir \"./staging\""
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "========================================="
echo "Building NeighborTools for Production"
echo "========================================="
echo "API URL: $API_URL"
echo "Environment: $ENVIRONMENT"
echo "Output Directory: $OUTPUT_DIR"
echo "========================================="

# Validate API URL format
if [[ ! "$API_URL" =~ ^https?:// ]]; then
    echo "❌ Error: API URL must start with http:// or https://"
    echo "   Example: ./build-for-production.sh --api-url \"https://api.yourapp.com\""
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build Backend
echo ""
echo "1️⃣ Building Backend..."
echo "======================"
echo ""

if [ ! -d "backend" ]; then
    echo "❌ Backend directory not found. Are you in the NeighborTools root directory?"
    exit 1
fi

cd backend/src/ToolsSharing.API

# Check if config.json exists
if [ ! -f "config.json" ]; then
    echo "❌ config.json not found in backend/src/ToolsSharing.API/"
    echo "   Please create config.json with your production settings"
    echo "   You can copy from config.sample.json and update with actual values"
    exit 1
fi

echo "🔨 Building backend..."
dotnet publish -c Release -o "../../../$OUTPUT_DIR/api"

if [ $? -ne 0 ]; then
    echo "❌ Backend build failed!"
    exit 1
fi

echo "✅ Backend build completed"

cd ../../../

# Build Frontend
echo ""
echo "2️⃣ Building Frontend..."
echo "======================="
echo ""

if [ ! -d "frontend" ]; then
    echo "❌ Frontend directory not found. Are you in the NeighborTools root directory?"
    exit 1
fi

cd frontend

echo "🔧 Configuring frontend..."
if [ -f "configure.sh" ]; then
    ./configure.sh --api-url "$API_URL" --environment "$ENVIRONMENT"
    if [ $? -ne 0 ]; then
        echo "❌ Frontend configuration failed!"
        exit 1
    fi
else
    echo "❌ configure.sh not found in frontend directory"
    exit 1
fi

echo "🔨 Publishing frontend..."
dotnet publish -c Release -o "../$OUTPUT_DIR/frontend"

if [ $? -ne 0 ]; then
    echo "❌ Frontend build failed!"
    exit 1
fi

echo "✅ Frontend build completed"

cd ..

# Create deployment info
echo ""
echo "3️⃣ Creating deployment info..."
echo "=============================="
echo ""

cat > "$OUTPUT_DIR/deployment-info.json" << EOF
{
  "buildDate": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "apiUrl": "$API_URL",
  "environment": "$ENVIRONMENT",
  "version": "$(date +"%Y.%m.%d")",
  "components": {
    "backend": {
      "path": "./api",
      "entrypoint": "ToolsSharing.API.dll"
    },
    "frontend": {
      "path": "./frontend",
      "entrypoint": "wwwroot"
    }
  }
}
EOF

echo "✅ Production build completed successfully!"
echo ""
echo "📁 Build output: $OUTPUT_DIR/"
echo "   📦 Backend: $OUTPUT_DIR/api/"
echo "   🌐 Frontend: $OUTPUT_DIR/frontend/"
echo "   📋 Deployment info: $OUTPUT_DIR/deployment-info.json"
echo ""
echo "🚀 Ready for deployment!"
echo "   Backend: Deploy contents of '$OUTPUT_DIR/api/' to your API server"
echo "   Frontend: Deploy contents of '$OUTPUT_DIR/frontend/wwwroot/' to your web server/CDN"