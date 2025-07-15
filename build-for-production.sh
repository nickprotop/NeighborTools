#!/bin/bash

# Production build script for NeighborTools
API_URL=${1:-"https://api.neighbortools.com"}
ENVIRONMENT=${2:-"Production"}
OUTPUT_DIR=${3:-"./publish"}

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
    echo "   Example: ./build-for-production.sh \"https://api.yourapp.com\""
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

echo "🔨 Building frontend with production configuration..."
if [ -f "build.sh" ]; then
    ./build.sh "$API_URL" "$ENVIRONMENT" "Release"
else
    echo "❌ build.sh not found in frontend directory"
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