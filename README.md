# NeighborTools - Community Tool Sharing Platform

A modern full-stack application for community tool sharing, built with .NET 8 backend and Blazor WebAssembly frontend.

## 🚀 Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd NeighborTools
   ```

2. **Start all services**
   ```bash
   ./start-services.sh
   ```

This will start both backend and frontend services with interactive setup.

## 📖 Project Overview

NeighborTools enables community members to share tools with each other through a modern web platform featuring:

- **Tool Catalog** - Browse available tools with detailed descriptions and images
- **Rental System** - Request tool rentals with approval workflow
- **User Management** - Secure registration and authentication
- **My Tools** - Manage your own tool listings
- **Rental History** - Track rental requests and history

## 🏗️ Architecture

### Backend (.NET 8 API)
- **Clean Architecture** with Core, Infrastructure, and API layers
- **JWT Authentication** with automatic token handling
- **Entity Framework Core** with MySQL database
- **AutoMapper** for object mapping
- **Redis** integration ready (future feature)
- **Docker support** with profiles for development

### Frontend (Blazor WebAssembly)
- **Component-based architecture** with reusable UI components
- **Automatic authentication** state management
- **Responsive design** with Bootstrap
- **Service-based architecture** for API communication
- **Local storage** for authentication persistence

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| Backend API | .NET 8, ASP.NET Core Web API |
| Frontend | Blazor WebAssembly |
| Database | MySQL 8.0 |
| Cache | Redis 7 (configured, not implemented) |
| Authentication | JWT Bearer tokens |
| ORM | Entity Framework Core |
| Containerization | Docker & Docker Compose |
| HTTP Client | HttpClientFactory with message handlers |

## 📁 Project Structure

```
NeighborTools/
├── backend/                    # .NET 8 API Backend
│   ├── src/
│   │   ├── ToolsSharing.API/          # Web API controllers and endpoints
│   │   ├── ToolsSharing.Core/         # Domain entities and business logic
│   │   └── ToolsSharing.Infrastructure/ # Data access and external services
│   ├── docker/                        # Docker configuration
│   ├── scripts/                       # Development scripts
│   └── tests/                         # Unit tests
├── frontend/                   # Blazor WebAssembly Frontend
│   ├── Pages/                         # Razor pages/components
│   ├── Services/                      # API communication services
│   ├── Layout/                        # Layout components
│   └── Models/                        # Frontend models
├── TODO_*.md                   # Future implementation tasks
└── start-services.sh          # Main development startup script
```

## 🔧 Development Setup

### Prerequisites
- **.NET 8 SDK** (required)
- **Docker & Docker Compose** (required)
- **Git** (for cloning)

### Backend Setup

```bash
cd backend

# Complete setup (run once)
./scripts/install.sh

# Daily development - choose API mode
./scripts/start-all.sh

# Infrastructure only (for debugging)
./scripts/start-infrastructure.sh
```

### Frontend Setup

```bash
cd frontend
dotnet run
```

## 📋 Available Scripts

### Backend Scripts (`backend/scripts/`)

**Setup & Start:**
- `install.sh` - Complete installation (infrastructure + migrations + seeding)
- `start-all.sh` - Daily development with API mode selection
- `start-infrastructure.sh` - Start MySQL & Redis only
- `dev-api.sh` - Start API with `dotnet run`
- `docker-api.sh` - Start API in Docker

**Stop & Cleanup:**
- `stop-api.sh` - Stop API (Docker or dotnet processes)
- `stop-infrastructure.sh` - Stop MySQL & Redis
- `stop-all.sh` - Stop everything, preserve data
- `uninstall.sh` - Complete removal (⚠️ deletes all data)

### Root Scripts
- `start-services.sh` - Start both backend and frontend

## 🌐 Service URLs

| Service | URL | Description |
|---------|-----|-------------|
| Frontend | http://localhost:5000 | Main application |
| Frontend (HTTPS) | https://localhost:5001 | Secure frontend |
| Backend API | http://localhost:5002 | API endpoints |
| Backend API (HTTPS) | https://localhost:5003 | Secure API |
| Swagger UI | http://localhost:5002/swagger | API documentation |

## 🔑 Default Accounts

The seeded database includes these test accounts:

| Email | Password | Role |
|-------|----------|------|
| john.doe@email.com | Password123! | User |
| jane.smith@email.com | Password123! | User |

## 🗄️ Database Schema

### Core Entities
- **Users** - User accounts with authentication
- **Tools** - Tool listings with owner relationships
- **Rentals** - Rental requests and approvals
- **Reviews** - User and tool reviews (future)

### Key Features
- **Soft deletion** for data integrity
- **Audit fields** (CreatedAt, UpdatedAt) on all entities
- **Navigation properties** for relationships
- **Indexes** for performance optimization

## 🔒 Security Features

- **JWT Authentication** with automatic token refresh
- **Password hashing** with secure algorithms
- **CORS configuration** for cross-origin requests
- **Input validation** throughout the application
- **Authenticated HTTP client** with automatic token injection

## 🚀 Deployment Modes

### Docker Mode (Production-like)
```bash
./scripts/start-all.sh  # Choose option 1
```
- API runs in Docker container
- Production-like environment
- Port 5002/5003 for API

### Development Mode
```bash
./scripts/start-all.sh  # Choose option 2 or 3
```
- API runs with `dotnet run` or `dotnet watch`
- Hot reload support
- Easier debugging

## 📊 Future Enhancements

See TODO files for planned features:
- `TODO_REDIS_IMPLEMENTATION.md` - Caching and session management
- `TODO_ORCHESTRATION_OBSERVABILITY.md` - Monitoring and observability

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Errors**
   ```bash
   # Restart infrastructure
   ./scripts/stop-infrastructure.sh
   ./scripts/start-infrastructure.sh
   ```

2. **Authentication Issues**
   - Check browser console for JWT token errors
   - Clear browser local storage
   - Restart both frontend and backend

3. **Port Conflicts**
   - Ensure ports 5000-5003, 3306, 6379 are available
   - Check with: `netstat -tulpn | grep :5000`

4. **Docker Issues**
   ```bash
   # View container logs
   cd backend/docker
   docker-compose logs mysql
   docker-compose logs redis
   ```

### Reset Everything
```bash
cd backend
./scripts/uninstall.sh  # ⚠️ Deletes all data
./scripts/install.sh    # Fresh installation
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License.

## 🆘 Support

- Check the troubleshooting section above
- Review API documentation at `/swagger`
- Create an issue in the repository
- Check container logs for debugging