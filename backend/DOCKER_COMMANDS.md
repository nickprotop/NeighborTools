# Docker Commands for Tools Sharing API

## Quick Start Options

### Option 1: Complete Stack (Recommended)
```bash
# Start everything (PostgreSQL, Redis, API)
./scripts/start-docker-full.sh

# Or manually:
cd docker
docker-compose up --build -d
```

### Option 2: API Only (Infrastructure running separately)
```bash
# Start just the API in Docker
./scripts/start-docker-api.sh

# Or manually:
cd docker
docker-compose up --build -d api
```

### Option 3: Step by Step
```bash
cd docker

# Start infrastructure only
docker-compose up -d postgresql redis

# Wait for PostgreSQL to be ready (15-30 seconds)
sleep 15

# Start API
docker-compose up --build -d api
```

## Common Docker Commands

### Service Management
```bash
cd docker

# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d api

# Stop all services
docker-compose down

# Stop specific service
docker-compose stop api

# Restart specific service
docker-compose restart api

# Rebuild and start (after code changes)
docker-compose up --build -d api
```

### Monitoring & Logs
```bash
# View all logs
docker-compose logs -f

# View API logs only
docker-compose logs -f api

# View PostgreSQL logs
docker-compose logs -f postgresql

# Check service status
docker-compose ps

# Check resource usage
docker stats
```

### Database Operations
```bash
# Connect to PostgreSQL container
docker-compose exec postgresql psql -U toolsuser -d toolssharing

# Run migrations in API container
docker-compose exec api dotnet ef database update

# Access Redis CLI
docker-compose exec redis redis-cli
```

### Development Workflow
```bash
# After code changes, rebuild and restart API
docker-compose up --build -d api

# View logs to check startup
docker-compose logs -f api

# Test API health
curl http://localhost:5000/health
```

## Access URLs

- **API HTTP**: http://localhost:5000
- **API HTTPS**: https://localhost:5001
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **PostgreSQL**: localhost:5433
- **Redis**: localhost:6379

## Troubleshooting

### API Won't Start
```bash
# Check API logs
docker-compose logs api

# Rebuild API container
docker-compose up --build -d api

# Check if ports are available
netstat -tulpn | grep :5000
```

### Database Connection Issues
```bash
# Check PostgreSQL status
docker-compose ps postgresql

# Test PostgreSQL connection
docker-compose exec postgresql psql -U toolsuser -d toolssharing -c "\l"

# Check API environment variables
docker-compose exec api env | grep CONNECTION
```

### Reset Everything
```bash
# Stop and remove all containers and volumes
docker-compose down -v

# Remove API image (forces rebuild)
docker rmi tools-sharing-api

# Start fresh
docker-compose up --build -d
```

## Environment Variables

The API container uses these environment variables:
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=https://+:5001;http://+:5000`
- `ConnectionStrings__DefaultConnection=server=mysql;port=3306;database=toolssharing;uid=toolsuser;pwd=ToolsPassword123!`
- `Redis__ConnectionString=redis:6379`