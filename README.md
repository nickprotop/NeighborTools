# NeighborTools

**Enterprise-grade community tool sharing platform with advanced security, payment processing, and bundle management.**

Built with .NET 9 and Blazor WebAssembly, NeighborTools provides a complete solution for tool sharing communities with professional-grade features including comprehensive security systems, payment processing, dispute resolution, and regulatory compliance.

## 🚀 Quick Start

```bash
# Complete setup (backend + frontend)
./setup-complete.sh

# Or manual setup
cd backend && ./scripts/install.sh
cd frontend && dotnet run
```

## ✨ Core Features

### 🔧 Tool & Bundle Management
- **Individual Tool Rentals** - Complete rental lifecycle management
- **Advanced Bundle System** - Curated tool collections for complete project solutions
- **Smart Availability** - Coordinated booking across multiple tools
- **Dynamic Pricing** - Bundle discounts and real-time cost calculation
- **Image Management** - Multi-image upload with MinIO object storage

### 💰 Payment & Financial Services
- **PayPal Integration** - Secure payment processing with webhook validation
- **Security Deposits** - Configurable deposit management
- **Platform Commission** - Automated fee calculation and collection
- **Payout Management** - Automated owner payments with configurable delays
- **Receipt Generation** - Professional PDF receipts with detailed breakdowns
- **Fraud Detection** - Advanced fraud prevention with velocity limits and suspicious activity monitoring

### 🛡️ Security & Compliance
- **Phase 3 Security System** - Multi-layered security with advanced analytics
- **Rate Limiting** - Configurable per-endpoint rate limiting with Redis backend
- **Brute Force Protection** - Advanced attack detection and prevention
- **IP Security** - Geographic filtering and malicious IP blocking
- **Session Management** - Device fingerprinting and session hijacking detection
- **Security Analytics** - Real-time threat monitoring with admin dashboard
- **GDPR Compliance** - Complete data protection and privacy management

### 🗣️ Communication & Disputes
- **Secure Messaging** - Real-time communication with automated content moderation
- **Dispute Resolution** - Complete dispute management with evidence upload
- **Mutual Closure** - Collaborative dispute resolution system
- **Email Notifications** - Comprehensive email system with professional templates

### 👤 User Experience
- **Favorites System** - Save tools and bundles for quick access
- **Public Profiles** - User profiles with rental history and ratings
- **Advanced Search** - Powerful filtering and discovery tools
- **Mobile Responsive** - Optimized for all device sizes
- **Dark Theme** - Complete dark mode support

### 🔒 Administration & Monitoring
- **Admin Dashboard** - Comprehensive platform management
- **Security Analytics** - Real-time threat monitoring and alerting
- **Performance Metrics** - System health monitoring with CPU/memory tracking
- **Sample Data Management** - Controlled test data for development
- **MinIO Management** - File storage administration
- **User Management** - Complete user lifecycle administration

## 🏗️ Architecture

### Backend (.NET 9)
- **Clean Architecture** - Separation of Core, Infrastructure, and API layers
- **CQRS Pattern** - Command/Query separation for scalable operations
- **Entity Framework Core** - MySQL database with comprehensive migrations
- **Mapster** - High-performance object mapping
- **MediatR** - Mediator pattern for loose coupling
- **Docker Support** - Complete containerization with multi-stage builds

### Frontend (Blazor WebAssembly)
- **Component Architecture** - Reusable UI components with MudBlazor 8.x
- **State Management** - Centralized authentication and application state
- **Service Layer** - Clean API communication with automatic token handling
- **Progressive Web App** - Offline support and app-like experience

### Infrastructure
- **MySQL 8.0** - Primary database with optimized indexes
- **Redis 7** - Distributed caching and rate limiting
- **MinIO** - S3-compatible object storage for files and images
- **Docker Compose** - Complete development environment orchestration

## 🛠️ Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **API** | .NET 9, ASP.NET Core | RESTful API with OpenAPI documentation |
| **Frontend** | Blazor WebAssembly | Client-side web application |
| **Database** | MySQL 8.0 | Primary data storage |
| **Cache** | Redis 7 | Rate limiting and performance |
| **Storage** | MinIO | File and image storage |
| **Authentication** | JWT + ASP.NET Identity | Secure user authentication |
| **Payments** | PayPal API | Payment processing |
| **Security** | Custom middleware stack | Multi-layered security system |
| **Monitoring** | Performance metrics | System health monitoring |

## 📋 Development Workflows

### Complete Development Environment
```bash
# Backend with hot reload + Frontend
cd backend && ./start-watch.sh
# In another terminal:
cd frontend && dotnet run
```

### Production Testing
```bash
# Backend in Docker + Frontend
cd backend && ./start-production.sh
# In another terminal:
cd frontend && dotnet run
```

### Granular Control
```bash
# Start storage services only
cd backend && ./scripts/storage/start.sh

# Start API (choose one)
./scripts/api/start-local.sh     # dotnet run
./scripts/api/start-watch.sh     # hot reload
./scripts/api/start-docker.sh    # Docker container
```

## 🌐 Service Endpoints

| Service | URL | Purpose |
|---------|-----|---------|
| **Frontend** | http://localhost:5000 | Main application |
| **API** | http://localhost:5002 | Backend services |
| **Swagger** | http://localhost:5002/swagger | API documentation |
| **MinIO Console** | http://localhost:9001 | File storage admin |

## 🔐 Security Features

### Multi-Layer Security System
- **Phase 1**: Request filtering, IP security, security headers
- **Phase 2**: Advanced rate limiting with Redis backend
- **Phase 3**: Session security, brute force protection, analytics

### Advanced Protection
- **Geographic Filtering** - Block/allow countries with IP geolocation
- **Attack Detection** - Velocity, distributed, and pattern-based attack detection
- **Session Monitoring** - Device fingerprinting and impossible travel detection
- **Token Security** - JWT blacklist and automatic rotation
- **Content Moderation** - Automated message content filtering

### Compliance & Privacy
- **GDPR Ready** - Complete data protection compliance
- **Cookie Consent** - Configurable consent management
- **Data Export** - Automated personal data export
- **Privacy Controls** - User data management and deletion rights

## 📊 Monitoring & Analytics

### Real-Time Metrics
- **System Health** - CPU, memory, response times
- **Security Threats** - Active threat detection and alerting
- **Performance** - Database queries, cache hit rates
- **Geographic Activity** - User location and threat mapping

### Administrative Tools
- **Security Dashboard** - Comprehensive threat monitoring
- **User Analytics** - Registration trends and activity patterns
- **System Status** - Infrastructure health monitoring
- **Alert Management** - Configurable security alerts

## 🎯 Default Configuration

### Admin Account
- **Email**: admin@neighbortools.com
- **Password**: Admin123!

### Test Accounts
Test accounts available through admin panel sample data management.

### Security Defaults
- Rate limiting: 2,000 requests per hour per endpoint
- Redis authentication enabled by default
- Security headers enforced
- Content moderation active

## 📚 Documentation

- **API Documentation**: Available at `/swagger` when running
- **Bundle System**: See `BUNDLE_SYSTEM_DOCUMENTATION.md`
- **Security Configuration**: See `backend/scripts/README.md`
- **Development Guide**: See `CLAUDE.md`

## 🔧 Configuration

### Environment Setup
```bash
# Complete interactive setup
./setup-complete.sh

# Backend configuration only
cd backend && ./scripts/install.sh

# View current configuration
cd backend && ./scripts/show-config.sh
```

### Post-Installation Configuration

After running the installation script, you can customize optional features by editing `backend/src/ToolsSharing.API/config.json`:

#### 💳 Payment Processing (Optional)
Configure PayPal integration for payment processing:

```json
{
  "Payment": {
    "PayPal": {
      "ClientId": "YOUR_PAYPAL_CLIENT_ID",
      "ClientSecret": "YOUR_PAYPAL_CLIENT_SECRET", 
      "Mode": "sandbox",  // or "live" for production
      "WebhookId": "YOUR_WEBHOOK_ID",
      "DisputeWebhookId": "YOUR_DISPUTE_WEBHOOK_ID",
      "IsEnabled": true
    }
  }
}
```

**PayPal Setup:**
1. Create a PayPal Developer account at https://developer.paypal.com
2. Create a new application to get Client ID and Secret
3. Configure webhooks for payment and dispute events
4. Update config.json with your credentials

#### 📧 Email Notifications (Optional)
Configure SMTP settings for email notifications:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yoursite.com",
    "FromName": "Your Platform Name"
  }
}
```

**SMTP Providers:**
- **Gmail**: smtp.gmail.com:587 (requires app password)
- **Outlook**: smtp-mail.outlook.com:587
- **SendGrid**: smtp.sendgrid.net:587
- **Custom**: Your SMTP provider settings

Leave `SmtpServer` empty to disable email sending (development mode).

#### 🛡️ Content Moderation (Optional)
Configure SightEngine API for automated content moderation:

```json
{
  "SightEngine": {
    "ApiUser": "YOUR_SIGHTENGINE_USER",
    "ApiSecret": "YOUR_SIGHTENGINE_SECRET",
    "Thresholds": {
      "NudityThreshold": 0.5,
      "OffensiveThreshold": 0.6,
      "ProfanityThreshold": 0.5
    }
  }
}
```

**SightEngine Setup:**
1. Sign up at https://sightengine.com/
2. Get API credentials from your dashboard
3. Configure detection thresholds (0.0-1.0)
4. Leave credentials empty to disable (basic moderation still active)

#### 🔒 Security Configuration (Pre-configured)
Advanced security settings with sensible defaults:

```json
{
  "RateLimit": {
    "EnableRateLimiting": true,
    "EndpointPolicies": {
      "/api/auth/login": { "RequestsPerWindow": 20 },
      "/api/*": { "RequestsPerWindow": 2000 }
    }
  },
  "IPSecurity": {
    "EnableIPBlocking": true,
    "BlockedCountries": ["CN", "RU"],  // ISO country codes
    "KnownMaliciousIPs": []
  },
  "BruteForceProtection": {
    "MaxFailedAttemptsBeforeLockout": 5,
    "AccountLockoutDuration": "00:15:00"
  }
}
```

#### 💰 Fraud Detection (Pre-configured)
Financial fraud prevention with configurable limits:

```json
{
  "FraudDetection": {
    "DailyAmountLimit": 5000.00,
    "HighRiskAmountThreshold": 2000.00,
    "AutoBlockRiskScore": 85.0
  }
}
```

#### 🏛️ GDPR Compliance (Pre-configured)
Privacy and data protection settings:

```json
{
  "GDPR": {
    "DataRetentionPeriodYears": 7,
    "CookieConsentExpiryDays": 365,
    "PrivacyPolicyVersion": "1.0"
  }
}
```

### Configuration Validation

After editing config.json, restart the backend to apply changes:

```bash
cd backend
./scripts/api/stop.sh
./scripts/api/start-local.sh  # or start-watch.sh
```

**Configuration Tips:**
- Essential services (MySQL, Redis, MinIO) are configured during installation
- Payment processing requires PayPal developer account
- Email notifications require SMTP provider or service
- Content moderation requires SightEngine API account  
- Security features work with default settings
- All optional features can be disabled by leaving credentials empty

## 🏆 Production Ready

NeighborTools includes enterprise-grade features for production deployment:

- **Security**: Multi-layered security system with threat detection
- **Scalability**: Redis caching and optimized database queries
- **Monitoring**: Performance metrics and health checks
- **Compliance**: GDPR-ready privacy and data protection
- **Reliability**: Comprehensive error handling and logging
- **Maintenance**: Automated database migrations and seeding

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Run GitLeaks setup: `./setup-gitleaks.sh`
4. Make your changes with comprehensive tests
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details.