# NeighborTools Scripts

## Service-Oriented Script Organization

### Storage Services (Always Docker)
- `./storage/start.sh` - Start MySQL, Redis, MinIO
- `./storage/stop.sh` - Stop storage services  
- `./storage/status.sh` - Show storage service status

### API Modes
- `./api/start-local.sh` - Start API with `dotnet run` (stable development)
- `./api/start-watch.sh` - Start API with `dotnet watch` (hot reload)
- `./api/start-docker.sh` - Start API in Docker (production-like)
- `./api/stop.sh` - Stop API (any mode)

### Complete Workflows
- `./start-dev.sh` - Storage + `dotnet run` (stable development)
- `./start-watch.sh` - Storage + `dotnet watch` (active development)
- `./start-production.sh` - Storage + Docker API (production testing)

## Common Development Workflows

**Most common (active development):**
```bash
./start-watch.sh  # Storage + hot reload API
```

**Stable development/debugging:**
```bash
./start-dev.sh    # Storage + dotnet run
```

**Production-like testing:**
```bash
./start-production.sh  # Storage + Docker API
```

**Granular control:**
```bash
./storage/start.sh     # Start just storage
./api/start-watch.sh   # Then start API with hot reload
```

## Legacy Scripts (Backup)
Original scripts are preserved in `./backup/` folder:
- `backup/start-all.sh`
- `backup/start-infrastructure.sh` 
- `backup/dev-api.sh`
- `backup/docker-api.sh`
- etc.

## Other Scripts
- `install.sh` - Initial setup and configuration
- `uninstall.sh` - Complete cleanup and removal
- `show-config.sh` - Show current configuration
- `test.sh` - Run test suite