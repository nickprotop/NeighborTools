#!/bin/bash

# Frontend build script with configuration injection
API_URL=${1:-"http://localhost:5002"}
ENVIRONMENT=${2:-"Development"}
BUILD_CONFIG=${3:-"Release"}

echo "========================================="
echo "Building NeighborTools Frontend"
echo "========================================="
echo "API URL: $API_URL"
echo "Environment: $ENVIRONMENT"
echo "Build Configuration: $BUILD_CONFIG"
echo "========================================="

# Validate API URL format
if [[ ! "$API_URL" =~ ^https?:// ]]; then
    echo "❌ Error: API URL must start with http:// or https://"
    echo "   Example: ./build.sh \"https://api.yourapp.com\""
    exit 1
fi

# Create config.json with actual values
echo "📝 Creating config.json..."
cat > config.json << EOF
{
  "ApiSettings": {
    "BaseUrl": "$API_URL",
    "TimeoutSeconds": 30,
    "RetryAttempts": 3
  },
  "Environment": "$ENVIRONMENT",
  "Features": {
    "EnableAdvancedSearch": true,
    "EnableNotifications": true,
    "EnablePayments": true,
    "EnableDisputes": true,
    "EnableAnalytics": $([ "$ENVIRONMENT" == "Production" ] && echo "true" || echo "false")
  }
}
EOF

# Copy config to wwwroot so it's served with the app
echo "📁 Copying configuration to wwwroot..."
mkdir -p wwwroot
cp config.json wwwroot/

# Build the application
echo "🔨 Building Blazor WebAssembly application..."
dotnet build --configuration $BUILD_CONFIG

if [ $? -eq 0 ]; then
    echo "✅ Frontend built successfully!"
    echo "   API URL: $API_URL"
    echo "   Environment: $ENVIRONMENT"
    echo "   Configuration: wwwroot/config.json"
else
    echo "❌ Build failed!"
    exit 1
fi