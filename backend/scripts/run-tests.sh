#!/bin/bash

# NeighborTools Test Runner Script
# Runs the comprehensive test suite for the ToolsSharing project

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}🧪 NeighborTools Test Suite${NC}"
echo "================================"

# Navigate to test directory
cd "$(dirname "$0")/../tests/ToolsSharing.Tests"

echo -e "${YELLOW}📦 Restoring test dependencies...${NC}"
dotnet restore

echo -e "${YELLOW}🔨 Building test project...${NC}"
dotnet build --no-restore

echo -e "${YELLOW}🧪 Running tests...${NC}"
dotnet test --no-build --verbosity normal --logger:"console;verbosity=detailed"

# Run tests with coverage if requested
if [[ "$1" == "--coverage" ]]; then
    echo -e "${YELLOW}📊 Running tests with coverage...${NC}"
    dotnet test --no-build --collect:"XPlat Code Coverage" --logger:"console;verbosity=minimal"
    
    # Install and run report generator if available
    if command -v reportgenerator &> /dev/null; then
        echo -e "${YELLOW}📈 Generating coverage report...${NC}"
        reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
        echo -e "${GREEN}Coverage report generated at: TestResults/CoverageReport/index.html${NC}"
    else
        echo -e "${YELLOW}⚠️  ReportGenerator not installed. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool${NC}"
    fi
fi

# Run performance tests if requested
if [[ "$1" == "--performance" ]]; then
    echo -e "${YELLOW}⚡ Running performance tests...${NC}"
    dotnet test --no-build --filter="Category=Performance" --verbosity normal
fi

echo -e "${GREEN}✅ Test suite completed successfully!${NC}"