# Tools Sharing Backend

A complete .NET 9 backend application for a tools sharing platform, built with microservices architecture, Entity Framework Core, MySQL, and Redis.

## Architecture

This application follows Clean Architecture principles with the following layers:

- **ToolsSharing.API** - Web API layer with controllers and endpoints
- **ToolsSharing.Core** - Domain entities, interfaces, and business logic
- **ToolsSharing.Infrastructure** - Data access, repositories, and external services

## Features

- **User Management** - Registration, authentication, and authorization with JWT
- **Tool Management** - CRUD operations for tools with images and categories
- **Rental System** - Tool rental requests, approvals, and tracking
- **Review System** - User and tool reviews with ratings
- **Load Balancing** - YARP reverse proxy for microservices
- **Caching** - Redis for performance optimization
- **Database** - MySQL with Entity Framework Core
- **Containerization** - Docker support for all services
- **API Documentation** - Swagger/OpenAPI integration
- **Logging** - Structured logging with Serilog
- **Health Checks** - Built-in health monitoring

## Technology Stack

- **.NET 9** - Framework
- **ASP.NET Core Web API** - REST API
- **Entity Framework Core 9** - ORM with MySQL provider
- **MySQL 8.0** - Primary database
- **Redis** - Caching and session storage
- **YARP** - Reverse proxy and load balancing
- **AutoMapper** - Object mapping
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Input validation
- **JWT Bearer** - Authentication
- **Serilog** - Structured logging
- **Docker** - Containerization

## Prerequisites

- .NET SDK 9.0 or later
- Docker and Docker Compose
- Git (for cloning)

## Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd backend
   ```

2. **Install dependencies**
   ```bash
   ./scripts/install.sh
   ```

3. **Run the complete setup**
   ```bash
   ./scripts/run-all.sh
   ```

4. **Start the API**
   ```bash
   ./scripts/start-api.sh
   ```

5. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/health

## Manual Setup

### 1. Infrastructure Setup

Start MySQL and Redis:
```bash
./scripts/start-infrastructure.sh
```

### 2. Database Migration

Run Entity Framework migrations:
```bash
./scripts/run-migrations.sh
```

### 3. Seed Database

Insert sample data:
```bash
./scripts/seed-data.sh
```

### 4. Start API

Launch the API server:
```bash
./scripts/start-api.sh
```

## Configuration

### Database Connection

Update `appsettings.json` for database connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=toolssharing;uid=toolsuser;pwd=ToolsPassword123!"
  }
}
```

### JWT Configuration

Configure JWT settings in `appsettings.json`:
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "ToolsSharing.API",
    "Audience": "ToolsSharing.Client",
    "ExpiresInMinutes": 60
  }
}
```

### Load Balancing

YARP configuration for multiple API instances:
```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "api-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "Destinations": {
          "api1": {
            "Address": "http://localhost:8080/"
          },
          "api2": {
            "Address": "http://localhost:8081/"
          }
        }
      }
    }
  }
}
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/forgot-password` - Password reset request
- `POST /api/auth/reset-password` - Password reset

### Tools
- `GET /api/tools` - List tools with filtering
- `GET /api/tools/{id}` - Get tool details
- `POST /api/tools` - Create new tool (authenticated)
- `PUT /api/tools/{id}` - Update tool (authenticated)
- `DELETE /api/tools/{id}` - Delete tool (authenticated)

### Rentals
- `GET /api/rentals` - List user rentals (authenticated)
- `GET /api/rentals/{id}` - Get rental details (authenticated)
- `POST /api/rentals` - Create rental request (authenticated)
- `PATCH /api/rentals/{id}/approve` - Approve rental (authenticated)
- `PATCH /api/rentals/{id}/reject` - Reject rental (authenticated)

## Database Schema

### Core Entities

- **Users** - User accounts with Identity integration
- **Tools** - Tool listings with details and availability
- **ToolImages** - Multiple images per tool
- **Rentals** - Rental requests and tracking
- **Reviews** - User and tool reviews with ratings

### Key Relationships

- Users can own multiple Tools
- Tools can have multiple ToolImages
- Rentals link Tools with Users (renter/owner)
- Reviews can be for Tools or Users

## Docker Services

The application includes these Docker services:

- **MySQL** - Primary database (port 3306)
- **Redis** - Cache and session store (port 6379)
- **API** - .NET application (port 8080)

### Docker Commands

```bash
# Start all services
docker-compose up -d

# Start only infrastructure
docker-compose up -d mysql redis

# Stop all services
docker-compose down

# Remove all data
docker-compose down -v

# View logs
docker-compose logs -f api
```

## Development

### Project Structure

```
backend/
├── src/
│   ├── ToolsSharing.API/          # Web API project
│   ├── ToolsSharing.Core/         # Domain layer
│   └── ToolsSharing.Infrastructure/ # Data access layer
├── tests/
│   └── ToolsSharing.Tests/        # Unit tests
├── docker/
│   ├── docker-compose.yml         # Docker configuration
│   ├── Dockerfile                 # API container
│   └── init-scripts/              # Database initialization
└── scripts/                       # Setup and run scripts
```

### Building the Solution

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/ToolsSharing.API
```

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/ToolsSharing.Infrastructure --startup-project src/ToolsSharing.API

# Update database
dotnet ef database update --project src/ToolsSharing.Infrastructure --startup-project src/ToolsSharing.API

# Remove last migration
dotnet ef migrations remove --project src/ToolsSharing.Infrastructure --startup-project src/ToolsSharing.API
```

## Security Features

- **JWT Authentication** - Secure token-based authentication
- **Password Hashing** - BCrypt password hashing
- **CORS Configuration** - Cross-origin request handling
- **Input Validation** - FluentValidation for request validation
- **Soft Delete** - Logical deletion for data integrity
- **Rate Limiting** - Built-in protection against abuse

## Performance Features

- **Redis Caching** - Fast data retrieval
- **Database Indexing** - Optimized query performance
- **Pagination** - Efficient data loading
- **Load Balancing** - YARP reverse proxy
- **Connection Pooling** - Database connection optimization

## Monitoring and Observability

- **Health Checks** - `/health` endpoint for monitoring
- **Structured Logging** - Serilog with JSON output
- **Request Tracing** - Built-in request logging
- **Error Handling** - Global exception handling

## Sample Data

The seed script includes:

- **Users**: john.doe@email.com, jane.smith@email.com (Password: Password123!)
- **Tools**: Professional drill, circular saw, ladder, pressure washer
- **Rentals**: Sample rental requests and approvals
- **Reviews**: User and tool reviews with ratings

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Ensure MySQL container is running
   - Check connection string in appsettings.json
   - Verify database exists and user permissions

2. **Migration Errors**
   - Ensure EF Core tools are installed
   - Check database server is accessible
   - Verify connection string format

3. **Docker Issues**
   - Ensure Docker is running
   - Check port conflicts (3306, 6379, 8080)
   - Review container logs: `docker-compose logs`

4. **JWT Token Issues**
   - Verify SecretKey in configuration
   - Check token expiration settings
   - Ensure proper Authorization header format

### Useful Commands

```bash
# Check Docker containers
docker ps

# View container logs
docker logs tools-sharing-mysql
docker logs tools-sharing-redis

# Connect to MySQL
docker exec -it tools-sharing-mysql mysql -u toolsuser -p

# Connect to Redis
docker exec -it tools-sharing-redis redis-cli

# Check API health
curl http://localhost:5000/health
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions and support:
- Create an issue in the repository
- Check the troubleshooting section
- Review the API documentation at `/swagger`