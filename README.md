# NeighborTools - Community Tool Sharing Platform

A modern full-stack application for community tool sharing, built with .NET 9 backend and Blazor WebAssembly frontend.

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

### Backend (.NET 9 API)
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
| Backend API | .NET 9, ASP.NET Core Web API |
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
├── backend/                    # .NET 9 API Backend
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
├── .github/workflows/          # GitHub Actions workflows
│   └── gitleaks.yml                   # Secret scanning workflow
├── .gitleaks.toml             # GitLeaks configuration
├── setup-gitleaks.sh          # GitLeaks setup script for new developers
├── TODO_*.md                   # Future implementation tasks
└── start-services.sh          # Main development startup script
```

## 🔧 Development Setup

### Prerequisites
- **.NET 9 SDK** (required)
- **Docker & Docker Compose** (required)
- **Git** (for cloning)
- **GitLeaks** (recommended for secret scanning)

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
- **Secret scanning** with GitLeaks integration

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

## 🔐 Secret Scanning with GitLeaks

NeighborTools includes GitLeaks integration to prevent secrets from being accidentally committed to version control.

### 🛡️ What's Included

- **Pre-commit hook** - Automatically scans staged changes before each commit
- **GitHub Actions workflow** - Runs on push, pull requests, and weekly schedule
- **Custom configuration** - Tailored rules for .NET/Blazor projects with allowlist for test values
- **Multiple output formats** - JSON, CSV, and SARIF reports

### 📦 Installation Options

#### Option 1: Local GitLeaks Installation (Recommended)

**Ubuntu/WSL:**
```bash
sudo apt update && sudo apt install gitleaks
```

**macOS:**
```bash
brew install gitleaks
```

**Windows:**
```bash
# Via chocolatey
choco install gitleaks

# Via scoop
scoop install gitleaks
```

#### Option 2: Docker-only Setup

If you prefer not to install GitLeaks locally, you can use the Docker-based hook:

```bash
# Copy the Docker hook to replace the standard one
cp .git/hooks/pre-commit-docker .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

This requires Docker but no local GitLeaks installation.

### 🚀 Usage

#### Manual Scanning

```bash
# Scan entire repository
gitleaks detect --verbose

# Scan staged changes only (before commit)
gitleaks protect --verbose

# Generate detailed report
gitleaks detect --report-format=json --report-path=gitleaks-report.json

# Use custom configuration
gitleaks detect --config=.gitleaks.toml --verbose
```

#### Automatic Scanning

**Pre-commit Hook:**
- ✅ Already configured
- Runs automatically on every `git commit`
- Blocks commits if secrets are detected
- Provides clear feedback on what was found

**GitHub Actions:**
- ✅ Already configured in `.github/workflows/gitleaks.yml`
- Runs on push to main branches
- Runs on pull requests
- Weekly scheduled scans
- Uploads reports as artifacts

### ⚙️ Configuration

The repository includes a custom `.gitleaks.toml` configuration with:

**Custom Rules:**
- Database connection strings
- PayPal client secrets
- JWT signing keys
- SMTP passwords

**Allowlist:**
- Test and example files
- Placeholder values (`your-password`, `example.com`, etc.)
- Binary files and build artifacts
- Development-specific patterns

### 🔧 Team Setup

**For New Developers:**

1. **Quick Setup** (recommended):
   ```bash
   # Run the automated setup script
   ./setup-gitleaks.sh
   ```

2. **Manual Install** (alternative):
   ```bash
   # Ubuntu/WSL
   sudo apt install gitleaks
   
   # macOS
   brew install gitleaks
   
   # Or use Docker-based hook (no installation needed)
   cp .git/hooks/pre-commit-docker .git/hooks/pre-commit
   chmod +x .git/hooks/pre-commit
   ```

3. **Verify Setup:**
   ```bash
   # Test the hook
   echo "test" > test.txt
   git add test.txt
   git commit -m "Test commit"  # Should show GitLeaks scan
   git reset HEAD~1 && rm test.txt  # Cleanup
   ```

4. **Understand the Workflow:**
   - Commits are automatically scanned
   - If secrets are detected, the commit is blocked
   - Remove the secrets and try again
   - Use environment variables for real secrets

### 🆘 Troubleshooting

**Pre-commit Hook Issues:**

```bash
# Check if hook is executable
ls -la .git/hooks/pre-commit

# Test hook manually
./.git/hooks/pre-commit

# Re-enable hook if disabled
chmod +x .git/hooks/pre-commit
```

**False Positives:**

If GitLeaks flags safe content, you can:

1. **Add to allowlist** in `.gitleaks.toml`:
   ```toml
   regexes = [
       '''your-safe-pattern'''
   ]
   ```

2. **Create baseline** to ignore existing issues:
   ```bash
   gitleaks detect --report-path=.gitleaks-baseline.json
   gitleaks detect --baseline-path=.gitleaks-baseline.json
   ```

**Bypass Hook (Emergency Only):**
```bash
git commit --no-verify -m "Emergency commit"
```
⚠️ **Only use in emergencies and scan manually afterward!**

### 📋 Security Best Practices

✅ **DO:**
- Use environment variables for secrets
- Store secrets in secure vaults (Azure Key Vault, AWS Secrets Manager)
- Rotate any exposed secrets immediately
- Run manual scans before major releases
- Train team members on secret management

❌ **DON'T:**
- Commit real API keys, passwords, or tokens
- Ignore GitLeaks warnings without investigation
- Disable the pre-commit hook permanently
- Store secrets in configuration files

### 📊 Integration Status

| Integration | Status | Description |
|-------------|---------|-------------|
| Pre-commit Hook | ✅ Active | Scans every commit automatically |
| GitHub Actions | ✅ Configured | Runs on push/PR/schedule |
| Custom Rules | ✅ Configured | .NET/Blazor specific patterns |
| Allowlist | ✅ Configured | Ignores test/placeholder values |
| Docker Support | ✅ Available | No local installation needed |

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