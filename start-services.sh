#!/bin/bash

echo "=== Starting NeighborTools Services ==="
echo ""

echo "1. Starting Backend API on ports 5000/5001..."
echo "   Command: cd backend && ./run-all.sh"
echo ""

echo "2. Starting Frontend on ports 5003/5004..."
echo "   Command: cd frontend && dotnet run"
echo ""

echo "=== Service URLs ==="
echo "Backend API:"
echo "  - HTTP:  http://0.0.0.0:5000"
echo "  - HTTPS: https://0.0.0.0:5001"
echo "  - Swagger: http://0.0.0.0:5000/swagger"
echo ""
echo "Frontend:"
echo "  - HTTP:  http://0.0.0.0:5003"
echo "  - HTTPS: https://0.0.0.0:5004"
echo ""

echo "=== Test Commands ==="
echo "Test API directly:"
echo "  curl -X POST http://0.0.0.0:5000/api/auth/login \\"
echo "    -H \"Content-Type: application/json\" \\"
echo "    -d '{\"email\":\"john.doe@email.com\",\"password\":\"Password123!\"}'"
echo ""

echo "=== Changes Made ==="
echo "✅ Frontend ports changed to 5003/5004"
echo "✅ Both services bind to 0.0.0.0 (all network interfaces)"
echo "✅ Frontend HttpClient configured to auto-detect API host"
echo "✅ CORS enabled in backend for cross-origin requests"
echo "✅ CORS middleware positioned correctly in pipeline"
echo ""

echo "If you're still getting 'Failed to fetch' errors:"
echo "1. Check if both services are running"
echo "2. Verify firewall allows ports 5000-5004"
echo "3. Test API directly with curl command above"
echo "4. Check browser developer console for detailed error"