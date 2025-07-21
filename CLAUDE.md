# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

NeighborTools is a **community tool sharing platform** with a .NET 9 Web API backend and Blazor WebAssembly frontend. Clean Architecture with:
- **ToolsSharing.Core** - Domain entities, commands/queries, and interfaces
- **ToolsSharing.Infrastructure** - Data access, Mapster, and external services  
- **ToolsSharing.API** - Controllers, JWT authentication, and API configuration

## ⚠️ CRITICAL: MudBlazor Version 8.x Requirements

**⚠️ BREAKING: Claude consistently follows outdated MudBlazor 7.x patterns causing build errors!**
**The frontend uses MudBlazor 8.9.0 - Claude MUST ALWAYS consult MudBlazor 8.x documentation, NEVER 7.x patterns!**

### ⚠️ CRITICAL API Changes in MudBlazor 8.x:

#### Dialog Management (MOST COMMON ERROR SOURCE):
- **Dialog Structure**: Each dialog must be a separate .razor file with `<MudDialog>` as root element
- **Dialog Injection**: Use `[CascadingParameter] IMudDialogInstance MudDialog { get; set; }` (IMudDialogInstance is the correct interface)
- **Dialog Closing**: Use `MudDialog.Close(DialogResult.Ok(data))` and `MudDialog.Cancel()` directly (no null checks needed)
- **Dialog Showing**: Use `DialogService.Show<DialogComponent>(title, parameters, options)` (NOT ShowAsync)
- **Dialog Parameters**: Use `new DialogParameters { { "ParamName", value } }` or `new DialogParameters().Add("ParamName", value)` (both work)

#### Component Property Changes:
- **Switch Components**: Use `@bind-Value` instead of `@bind-Checked`
- **Date Picker Events**: Use `@bind-Date:after` instead of `OnDateChanged`  
- **Icon References**: Always prefix with `@` (e.g., `Icon="@Icons.Material.Filled.Save"`)
- **MudSelect**: Use `T="Type"` for value type declaration
- **MudTable**: Items property binding: `Items="@items"` not `ServerData`

#### Table and Data Display:
- **MudTable**: Use `Items="@dataList"` for client-side data
- **Pagination**: Use `MudPagination` with `Selected` and `SelectedChanged` parameters
- **Loading States**: Use `Loading="@isLoading"` parameter on components

### Complete Dialog Pattern:
```csharp
// Caller component
@inject IDialogService DialogService

private Task OpenDialog()
{
    var parameters = new DialogParameters { { "ParamName", value } };
    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = DialogService.Show<MyDialog>("Dialog Title", parameters, options);
    return dialog.Result;
}

// Dialog component (separate .razor file)
<MudDialog>
    <TitleContent>Dialog Title</TitleContent>
    <DialogContent>Dialog Content</DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel" StartIcon="@Icons.Material.Filled.Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit" StartIcon="@Icons.Material.Filled.Save">OK</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
    [Parameter] public string ParamName { get; set; }

    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => MudDialog.Cancel();
}
```

### Always Check Current Documentation:
- **Primary Source**: https://mudblazor.com/components/
- **Migration Guide**: https://github.com/MudBlazor/MudBlazor/issues/9953
- **When in doubt**: Search "MudBlazor 8.x [component name]" to verify current API

### ⚠️ CRITICAL: Provider Configuration
**NEVER duplicate MudBlazor providers!** Only declare providers once in `App.razor`:
```csharp
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

**Common Issue**: Duplicate `MudPopoverProvider` or `MudDialogProvider` in both `App.razor` and `MainLayout.razor` causes:
- Dialog closing failures (background overlay changes but dialog stays open)
- Error: "There is already a subscriber to the content with the given section ID 'mud-overlay-to-popover-provider'"
- Conflicting overlay state management

**Solution**: Remove all provider declarations from `MainLayout.razor` and keep only in `App.razor`.

## ⚠️ CRITICAL: Messaging Moderation Documentation Requirement

**⚠️ MANDATORY: Any changes to the messaging moderation system MUST update documentation!**

**The messaging system includes automated content moderation with legal and compliance implications. Any modifications to moderation code require documentation updates to maintain transparency and auditability.**

### ⚠️ CRITICAL: When to Update Documentation

**REQUIRED updates to `MESSAGING_MODERATION_WORKFLOW.md` for ANY changes to:**

#### Content Moderation Code:
- **MessageService.cs** (lines 62-68) - The "before dispatch" hook
- **ContentModerationService.cs** - Pattern matching, severity levels, validation logic
- **IContentModerationService.cs** - Interface changes or new DTOs
- **ProhibitedPatterns dictionary** - New patterns, severity changes, pattern removal
- **SuspiciousWords list** - New words or phrases added to detection

#### Admin Interface Changes:
- **MessagingManagement.razor** - New features, UI changes, workflow modifications
- **MessageReview.razor** - New actions, review process changes
- **AdminDashboard.razor** - Messaging alerts, statistics, or navigation changes

#### API Endpoint Changes:
- New admin messaging endpoints
- Moderation-related API modifications
- User enforcement endpoint changes

#### Configuration Changes:
- Severity level definitions
- User violation thresholds
- Auto-approval settings
- Pattern matching configuration

### Documentation Update Requirements:

1. **Technical Changes** - Update code references, line numbers, method signatures
2. **Process Changes** - Update workflow diagrams, decision trees, escalation procedures  
3. **Pattern Changes** - Document new prohibited content patterns with examples and severity
4. **Policy Changes** - Update enforcement guidelines, user communication templates
5. **API Changes** - Update endpoint documentation with examples

### Compliance Note:
**Moderation system changes without documentation updates can:**
- Create legal liability for undocumented content policies
- Fail regulatory audits requiring transparent moderation processes
- Prevent proper staff training on moderation procedures
- Block effective troubleshooting of moderation issues

**ALWAYS update `MESSAGING_MODERATION_WORKFLOW.md` immediately after any moderation code changes.**

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
# Infrastructure only (for manual API debugging)
./backend/scripts/start-infrastructure.sh

# Start API manually after infrastructure is running
cd backend && dotnet run --project src/ToolsSharing.API

# DO NOT USE ./backend/scripts/start-all.sh - requires interactive input

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

### Docker Images
- **MySQL**: `mysql:8.0` (container: `tools-sharing-mysql`)
- **Redis**: `redis:7-alpine` (container: `tools-sharing-redis`)

### Configuration Files
- **Environment Variables**: `backend/docker/.env`
- **Docker Compose**: `backend/docker/docker-compose.yml`
- **Passwords**: All database passwords are stored in `backend/docker/.env` file

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
- **Payment** → processes financial transactions for rentals
- **Dispute** → handles rental conflicts with evidence and communication
- **FraudCheck** → monitors suspicious activity patterns
- All entities have audit fields (CreatedAt, UpdatedAt) and soft deletion

### Data Access Pattern
- **Repository Pattern**: Generic `IRepository<T>` with `Repository<T>` implementation
- **Unit of Work**: `IUnitOfWork` for transaction management
- **Mapster**: Object mapping between entities and DTOs (converted from records to classes to fix mapping issues)
- **CQRS Pattern**: Commands for writes, Queries for reads using record types

### Essential Development Practices
- **Always use strongly-typed DTOs instead of dynamic objects**
  - Provides compile-time type safety
  - Enables better tooling support and refactoring
  - Prevents runtime type-related errors
  - Improves API contract clarity and documentation
- **Always use secure way to identify admin, and not depend on queries (from frontend)**
  - Prevents potential security vulnerabilities
  - Ensures admin access is verified through robust authentication mechanisms

### Important: DTO Architecture
**All DTOs must be classes, not records** due to Mapster compatibility issues. When creating new DTOs:
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
- **Handlers**: Separate handler classes for each command/query

### Frontend Service Architecture
- **HttpClient Factory**: Named client with `AuthenticatedHttpClientHandler`
- **Service Layer**: `AuthService`, `ToolService`, `RentalService`, `PaymentService`, `DisputeService` for API communication
- **State Management**: Blazor authentication state with local storage persistence
- **UI Framework**: MudBlazor 8.9.0 for modern, responsive components
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

## Payment System Architecture

The application features a **comprehensive payment processing system** with PayPal integration, commission tracking, fraud detection, and dispute management:

### Payment Flow
1. **Rental Creation** → Payment required before approval
2. **PayPal Integration** → Secure payment processing via PayPal API
3. **Commission Calculation** → Automatic platform fee calculation
4. **Security Deposit** → Hold funds for tool protection
5. **Owner Payouts** → Automatic or manual payout processing
6. **Dispute Handling** → Comprehensive dispute resolution workflow

### Key Payment Services

#### Core Payment Services
- **`IPaymentService`** - Main payment processing interface
  - Location: `ToolsSharing.Infrastructure.Services.PaymentService`
  - Handles payment creation, processing, refunds, and status management
  - Integrates with PayPal API and fraud detection systems

- **`IPaymentProvider`** - Payment provider abstraction
  - Location: `ToolsSharing.Infrastructure.PaymentProviders.PayPalPaymentProvider`
  - Supports multiple payment providers (currently PayPal, extensible for Stripe)
  - Handles external API communication and webhook validation

- **`IPaymentStatusService`** - Payment status tracking and communication
  - Location: `ToolsSharing.Infrastructure.Services.PaymentStatusService`
  - Provides user-friendly status explanations and payout timelines
  - Manages payment lifecycle notifications

#### Security and Fraud Detection
- **`IFraudDetectionService`** - Advanced fraud prevention
  - Location: `ToolsSharing.Infrastructure.Services.FraudDetectionService`
  - Velocity limits, suspicious activity monitoring, AML compliance
  - Configurable rules and automatic blocking mechanisms

- **`IPayPalWebhookValidator`** - Webhook signature validation
  - Location: `ToolsSharing.Infrastructure.Security.PayPalWebhookValidator`
  - Prevents webhook fraud and ensures payment authenticity
  - Validates PayPal signature headers and request integrity

#### Financial Operations
- **`IPaymentReceiptService`** - Receipt generation and tracking
  - Location: `ToolsSharing.Infrastructure.Services.PaymentReceiptService`
  - Generates professional receipts with detailed breakdowns
  - Tracks receipt delivery and payment confirmations

- **Payout Background Service** - Automated owner payments
  - Location: `ToolsSharing.API.Services.PayoutBackgroundService`
  - Scheduled processing of owner payouts after rental completion
  - Configurable delay and retry mechanisms

### Payment Configuration
```json
{
  "Payment": {
    "PayPal": {
      "ClientId": "...",
      "ClientSecret": "...",
      "Environment": "Sandbox", // or "Live"
      "WebhookId": "..."
    },
    "Commission": {
      "PlatformFeePercentage": 5.0,
      "MinimumFee": 0.50
    },
    "SecurityDeposit": {
      "DefaultPercentage": 20.0,
      "MaximumAmount": 500.00
    }
  }
}
```

## Dispute Management System

Comprehensive dispute resolution system with evidence upload, communication, and PayPal integration:

### Dispute Workflow
1. **Dispute Creation** → Users can create disputes for rental issues
2. **Evidence Upload** → File upload system for supporting documentation
3. **Communication** → Message system between parties
4. **Escalation** → Admin review and PayPal dispute creation
5. **Resolution** → Final resolution with potential refunds
6. **Notifications** → Email notifications for all dispute events

### Key Dispute Services

#### Core Dispute Management
- **`IDisputeService`** - Main dispute processing interface
  - Location: `ToolsSharing.Infrastructure.Services.DisputeService`
  - Handles dispute creation, message management, evidence upload, and resolution
  - Integrates with PayPal dispute API for escalation

- **`IDisputeNotificationService`** - Comprehensive notification system
  - Location: `ToolsSharing.Infrastructure.Services.DisputeNotificationService`
  - Sends branded email notifications for all dispute events
  - Supports dispute creation, messages, status changes, escalation, resolution, evidence upload, and overdue reminders

#### File and Evidence Management
- **`IFileStorageService`** - File upload and storage abstraction
  - Location: `ToolsSharing.Infrastructure.Services.LocalFileStorageService`
  - Secure file validation, storage, and retrieval
  - Supports local storage with extensibility for cloud providers
  - File type validation, size limits, and secure naming

### Dispute Entity Structure
- **`Dispute`** - Main dispute entity with rental relationship
- **`DisputeMessage`** - Communication between parties and admins
- **`DisputeEvidence`** - File evidence with metadata and access control
- **`DisputeTimeline`** - Audit trail of all dispute activities

## Shared Services Architecture

### Email Notification System
- **`IEmailNotificationService`** - Comprehensive email system
  - Location: `ToolsSharing.Infrastructure.Services.EmailNotificationService`
  - Professional HTML templates for all email types
  - User preference management and unsubscribe handling
  - Queue management and delivery tracking
  - Template types: Authentication, Rentals, Payments, Disputes, Security, Marketing


### File Storage Service
- **`IFileStorageService`** - Universal file management
  - Location: `ToolsSharing.Infrastructure.Services.LocalFileStorageService`
  - Used by dispute evidence, tool images, and user uploads
  - Configurable validation rules and storage providers
  - Security features: file type validation, size limits, secure paths


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
1. **High Priority**: Basic commission system, advanced features
2. **Medium Priority**: Cloud deployment automation, Redis implementation
3. **Lower Priority**: Mobile app, advanced monetization features

**Recently Completed** (Major features only - see CHANGELOG.md for full history):
- ✅ .NET 9 upgrade with WSL compatibility 
- ✅ MudBlazor 8.9.0 upgrade with dialog API fixes
- ✅ Complete payment system with PayPal integration
- ✅ Dispute management with evidence upload
- ✅ Admin management interfaces
- ✅ Messaging system with moderation
- ✅ Favorites system with UI integration
- ✅ **Payment workflow UX improvements** (January 2025)
  - Fixed critical payment dialog cost calculation bug (100x multiplication issue)
  - Enhanced payment cost accuracy with proper backend API integration
  - Added "Pay Now to Show Commitment" functionality for pending rentals
  - Implemented clickable tool navigation across rental interfaces

See `TODO_MASTER_INDEX.md` for detailed timelines and resource recommendations.

## Important Development Notes

### Shell Script Line Endings
**ALWAYS create shell scripts (.sh files) with Unix line endings (LF, not CRLF)**
- Windows line endings cause "bad interpreter" errors on Linux/macOS
- Use Unix line endings even when developing on Windows (WSL compatibility)
- If line ending issues occur, fix with: `sed -i 's/\r$//' script.sh`


### Known Working Patterns
- **MudBlazor 8.x**: Use `new DialogParameters()` and `.Add()` method; `@bind-Date:after` for events
- **Authentication**: JWT tokens auto-injected via `AuthenticatedHttpClientHandler`
- **DTOs**: Must be classes (not records) for Mapster compatibility
- **Services**: All registered in DI, use `[Inject]` or constructor injection
- **Payment Cost Calculation**: Always use `PaymentService.CalculateRentalCostAsync()` API instead of local calculations
- **Payment Dialog**: Pass pre-calculated values from `RentalCostCalculationResponse` to avoid double fee calculation

## Common Troubleshooting

- **Authentication**: Check `AuthenticatedHttpClientHandler` and browser localStorage
- **Database**: MySQL container must be running; check `docker-compose logs mysql`
- **Services**: Auto-registered in `DependencyInjection.cs`; PayPal config required for payments

## Available Services

**Core**: `IAuthService`, `IToolsService`, `IRentalsService`, `IUserService`
**Payment**: `IPaymentService`, `IPaymentProvider`, `IFraudDetectionService`
**Communication**: `IDisputeService`, `IEmailNotificationService`, `IFileStorageService`, `IMessageService`
**Infrastructure**: `IRepository<T>`, `IUnitOfWork`, `IJwtTokenService`

## Implementation Workflow

1. Use TodoWrite tool to plan complex tasks
2. Search/understand codebase requirements  
3. Implement solution and verify with tests
4. **NEVER commit unless explicitly asked**
5. **MANDATORY**: Update `CHANGELOG.md` after completing significant features