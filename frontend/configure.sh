#!/bin/bash

# Frontend configuration script
# Usage: ./configure.sh [OPTIONS]
# 
# Options:
#   --api-url URL          API base URL (default: http://localhost:5002)
#   --environment ENV      Environment name (default: Development)
#   --help                 Show this help message
#
# Examples:
#   ./configure.sh
#   ./configure.sh --api-url "https://api.yourapp.com" --environment "Production"
#   ./configure.sh --environment "Staging"

# Set defaults
API_URL="http://localhost:5002"
ENVIRONMENT="Development"

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
        --help|-h)
            echo "Frontend configuration script"
            echo ""
            echo "Usage: ./configure.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --api-url URL          API base URL (default: http://localhost:5002)"
            echo "  --environment ENV      Environment name (default: Development)"
            echo "  --help, -h             Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./configure.sh"
            echo "  ./configure.sh --api-url \"https://api.yourapp.com\" --environment \"Production\""
            echo "  ./configure.sh --environment \"Staging\""
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
echo "Configuring NeighborTools Frontend"
echo "========================================="
echo "API URL: $API_URL"
echo "Environment: $ENVIRONMENT"
echo "========================================="

# Validate API URL format
if [[ ! "$API_URL" =~ ^https?:// ]]; then
    echo "❌ Error: API URL must start with http:// or https://"
    echo "   Example: ./configure.sh --api-url \"https://api.yourapp.com\""
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

echo "✅ Frontend configured successfully!"
echo "   API URL: $API_URL"
echo "   Environment: $ENVIRONMENT"
echo "   Configuration: wwwroot/config.json"
echo ""
echo "Next steps:"
echo "   dotnet run              # Start development server"
echo "   dotnet build            # Build application"
echo "   dotnet publish          # Publish for deployment"