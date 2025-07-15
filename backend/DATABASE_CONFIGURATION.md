# Database Configuration Guide

This document explains how to configure database passwords for the NeighborTools project.

## Overview

The project uses MySQL for the database and supports both default passwords (for development) and custom passwords (for production/security).

## Password Configuration Methods

### 1. Installation Script (Recommended)

The `install.sh` script will prompt you for database passwords during installation:

```bash
./scripts/install.sh
```

During installation, you'll be prompted:
- **MySQL root password**: Administrative password for MySQL
- **MySQL toolsuser password**: Password for the application user

Press Enter to use default passwords (recommended for development).

### 2. Direct .env File Editing

You can directly edit the `.env` file in the docker directory:

```bash
# Edit backend/docker/.env
MYSQL_ROOT_PASSWORD=YourRootPassword123!
MYSQL_USER_PASSWORD=YourUserPassword123!
```

Then run any script:
```bash
./scripts/start-all.sh
```

### 3. Manual Configuration

Edit `src/ToolsSharing.API/config.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=toolssharing;uid=toolsuser;pwd=YourPassword123!"
  }
}
```

## Default Passwords

If no custom passwords are provided, the system uses these defaults:

- **MySQL root password**: `RootPassword123!`
- **MySQL toolsuser password**: `ToolsPassword123!`

## How It Works

### Docker Compose Integration

The `docker-compose.yml` file uses environment variables:

```yaml
environment:
  MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
  MYSQL_PASSWORD: ${MYSQL_USER_PASSWORD}
```

The values are provided by a `.env` file that Docker Compose automatically reads:

```bash
# backend/docker/.env
MYSQL_ROOT_PASSWORD=RootPassword123!
MYSQL_USER_PASSWORD=ToolsPassword123!
```

This approach:
- ✅ Uses standard Docker Compose .env file mechanism
- ✅ No modifications needed to docker-compose.yml
- ✅ Won't break with future updates or cloud migrations
- ✅ Portable across different environments

### Backend Configuration

The backend configuration (`config.json`) is automatically updated with the database password during installation.

## Scripts That Support Password Configuration

All backend scripts automatically read and use the configured password:

- `install.sh` - Prompts for passwords and creates .env file
- `start-all.sh` - Reads passwords from .env file
- `start-infrastructure.sh` - Reads passwords from .env file
- `show-config.sh` - Shows current configuration from both .env and config.json

## Troubleshooting

### Check Current Configuration

```bash
./scripts/show-config.sh
```

### Reset to Defaults

1. Delete existing containers:
```bash
cd docker && docker-compose down -v
```

2. Run install script again:
```bash
./scripts/install.sh
```

### Update Password After Installation

1. Update both `.env` file and `config.json`:
```bash
# Edit backend/docker/.env
MYSQL_USER_PASSWORD=NewPassword123!

# Edit backend/src/ToolsSharing.API/config.json
# Update the connection string with new password
```

2. Restart infrastructure:
```bash
cd docker && docker-compose down -v
docker-compose --profile infrastructure up -d
```

## Security Recommendations

### Development
- Default passwords are fine for local development
- Use the install script for convenience

### Production
- Always use custom, strong passwords
- Never commit .env files to version control
- Consider using Docker secrets or cloud secret management
- Set up proper backup procedures for databases

### Best Practices
- Use complex passwords with special characters
- Rotate passwords regularly
- Use different passwords for root and application user
- Keep .env files secure and restricted (chmod 600)
- Use different .env files for different environments

## File Structure

```
backend/
├── src/ToolsSharing.API/
│   ├── config.json              # Contains database connection (created by install)
│   └── config.sample.json       # Template with placeholders
├── docker/
│   ├── docker-compose.yml       # Uses environment variables
│   ├── .env                     # Contains actual passwords (not in git)
│   └── .env.sample             # Template with defaults
└── scripts/
    ├── install.sh               # Prompts for passwords and creates .env
    ├── start-all.sh            # Reads from .env file
    ├── start-infrastructure.sh  # Reads from .env file
    └── show-config.sh          # Shows current configuration
```

## Migration from Hardcoded Passwords

If you're updating from an older version with hardcoded passwords:

1. The system will automatically create .env file with defaults if missing
2. Run `./scripts/install.sh` to set up custom passwords
3. No breaking changes - everything continues to work
4. The .env file approach is more portable and cloud-friendly

This ensures backward compatibility while providing security improvements and better cloud migration support.