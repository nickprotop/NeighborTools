# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

NeighborTools is a **community tool sharing platform** with a .NET 8 Web API backend and Blazor WebAssembly frontend. The backend follows **Clean Architecture** with three layers:

- **ToolsSharing.Core** - Domain entities, commands/queries, and interfaces
- **ToolsSharing.Infrastructure** - Data access, AutoMapper, and external services  
- **ToolsSharing.API** - Controllers, JWT authentication, and API configuration

## Essential Development Commands

### Initial Setup
```bash
# One-time complete setup (infrastructure + migrations + seeding)
./backend/scripts/install.sh

# Start both backend and frontend (recommended for full-stack development)
./start-services.sh

# Alternative: Start services separately
./backend/scripts/start-all.sh  # Backend only
cd frontend && dotnet run       # Frontend only
```

### Backend Development
```bash
# Daily development with interactive API mode selection
./backend/scripts/start-all.sh

# Infrastructure only (for manual API debugging)
./backend/scripts/start-infrastructure.sh

# Database migrations
dotnet ef migrations add MigrationName --project src/ToolsSharing.Infrastructure --startup-project src/ToolsSharing.API
dotnet ef database update --project src/ToolsSharing.Infrastructure --startup-project src/ToolsSharing.API

# Seed data only
dotnet run --project src/ToolsSharing.API --seed-only
```

### Service Management
```bash
# Stop API (handles both Docker and dotnet processes)
./backend/scripts/stop-api.sh

# Stop all services, preserve data
./backend/scripts/stop-all.sh

# Complete removal (⚠️ deletes all data)
./backend/scripts/uninstall.sh
```

### Testing
```bash
# Run all tests (basic xUnit structure exists, tests need implementation)
dotnet test

# Run tests from specific project
dotnet test backend/tests/ToolsSharing.Tests/

# Build entire solution (backend + frontend)
dotnet build

# Restore packages for entire solution
dotnet restore
```

## Service URLs and Configuration

- **Frontend**: http://localhost:5000 (HTTPS: 5001)
- **Backend API**: http://localhost:5002
- **Swagger**: http://localhost:5002/swagger
- **MySQL**: localhost:3306 (user: toolsuser, password: ToolsPassword123!)
- **Redis**: localhost:6379 (configured but not implemented)

## Authentication Architecture

The application uses **JWT Bearer tokens** with a sophisticated flow:

1. **Frontend**: `AuthenticatedHttpClientHandler` automatically injects tokens into all requests
2. **Backend**: ASP.NET Core Identity with JWT token generation and refresh
3. **Token Storage**: Local storage with "remember me" functionality
4. **State Management**: `CustomAuthenticationStateProvider` for Blazor authentication state

**Key Classes:**
- `AuthService` (frontend) - Authentication operations and token management with remember me functionality
- `AuthHandlers` (backend) - Login/register command handlers
- `JwtTokenService` - Token generation and validation
- `AuthenticatedHttpClientHandler` - Automatic JWT token injection for all HTTP requests

## Data Layer Patterns

### Entity Relationships
- **User** → owns multiple **Tools**
- **Tool** → has multiple **Rentals**  
- **Rental** → connects User (renter) with Tool (owner)
- All entities have audit fields (CreatedAt, UpdatedAt) and soft deletion

### Data Access Pattern
- **Repository Pattern**: Generic `IRepository<T>` with `Repository<T>` implementation
- **Unit of Work**: `IUnitOfWork` for transaction management
- **AutoMapper**: Object mapping between entities and DTOs (converted from records to classes to fix mapping issues)
- **CQRS Pattern**: Commands for writes, Queries for reads using record types

### Important: DTO Architecture
**All DTOs must be classes, not records** due to AutoMapper compatibility issues. When creating new DTOs:
```csharp
// ✅ Correct - Class with properties
public class ToolDto
{
    public string OwnerName { get; set; } = "";
    // ... other properties
}

// ❌ Incorrect - Record with default parameters
public record ToolDto(string OwnerName = "");
```

## Script System and Docker

### Docker Compose Profiles
- `infrastructure` - MySQL + Redis only
- `api` - API container only  
- `full` - Complete stack

### API Mode Selection
The `start-all.sh` script offers three modes:
1. **Docker mode** - Production-like environment (port 5002/5003)
2. **dotnet run** - Development mode (port 5000/5001)
3. **dotnet watch** - Hot reload development (port 5000/5001)

User preferences are saved in `.dev-mode` file for faster subsequent starts.

## Key Development Patterns

### API Response Structure
All endpoints return standardized responses:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### Command/Query Pattern
- **Commands**: Create/update operations (e.g., `CreateToolCommand`, `CreateRentalCommand`)
- **Queries**: Read operations (e.g., `GetToolsQuery`, `GetRentalByIdQuery`) 
- **Handlers**: MediatR pattern with separate handler classes

### Frontend Service Architecture
- **HttpClient Factory**: Named client with `AuthenticatedHttpClientHandler`
- **Service Layer**: `AuthService`, `ToolService`, `RentalService` for API communication
- **State Management**: Blazor authentication state with local storage persistence
- **UI Framework**: MudBlazor for modern, responsive components
- **Layout**: Mobile-responsive design with drawer navigation and modern header

## Database and Seeding

### Default Test Accounts
- john.doe@email.com / Password123!
- jane.smith@email.com / Password123!

### Migration Management
Migrations are in `src/ToolsSharing.Infrastructure/Migrations/`. The application automatically runs migrations on startup, and the install script seeds initial data.

### Entity Configuration
Entity configurations are in `src/ToolsSharing.Infrastructure/Configurations/` using Fluent API for relationships, indexes, and constraints.

## Authentication Flow Details

1. **Login** → `AuthController.Login` → Returns `AuthResult` with AccessToken + refresh tokens + user details
2. **Token Storage** → Frontend stores in localStorage with expiration and remember me option
3. **State Restoration** → Authentication state restored on app startup from localStorage
4. **Request Interception** → `AuthenticatedHttpClientHandler` adds Bearer header automatically
5. **Token Refresh** → Automatic refresh when access token expires
6. **Logout** → Clears localStorage and authentication state

## Error Handling and Validation

- **Global Exception Handler** in API layer returns consistent error responses
- **FluentValidation** for command/query validation (planned)
- **Frontend Error Handling** in service layer with user-friendly messages
- **Soft Deletion** pattern preserves data integrity

## Development Roadmap

### Comprehensive TODO Documentation
The project includes detailed TODO files with prioritized roadmap:
- **`TODO_MASTER_INDEX.md`** - Prioritized overview of all planned features
- **`TODO_DOTNET9_UPGRADE.md`** - .NET 9 upgrade plan (next priority)
- **`TODO_BASIC_COMMISSION_SYSTEM.md`** - MVP monetization features
- **`TODO_AUTOMATED_CLOUD_DEPLOYMENT.md`** - DevOps automation (Azure/DigitalOcean)
- **`TODO_REDIS_IMPLEMENTATION.md`** - Caching implementation
- **`TODO_ORCHESTRATION_OBSERVABILITY.md`** - Monitoring and observability
- **`TODO_MAUI_MOBILE_APP.md`** - Cross-platform mobile app
- **`TODO_MONETIZATION_PLATFORM.md`** - Advanced monetization platform

### Current Development Priorities
1. **High Priority**: .NET 9 upgrade, basic commission system
2. **Medium Priority**: Cloud deployment automation, Redis implementation
3. **Lower Priority**: Mobile app, advanced monetization features

See `TODO_MASTER_INDEX.md` for detailed timelines and resource recommendations.

## Common Troubleshooting

### Port Configuration Issues
- Frontend HttpClient is configured to detect and use correct backend URL
- CORS is configured for cross-origin requests between frontend/backend

### Authentication Token Issues
- Check `AuthenticatedHttpClientHandler` for automatic token injection
- Verify tokens in browser localStorage
- Authentication state persists across browser sessions via `CustomAuthenticationStateProvider`

### Database Connection Issues
- MySQL container must be running before API starts
- Connection string in `appsettings.json` should match Docker container settings
- Use `docker-compose logs mysql` for MySQL container debugging