# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

NeighborTools is a **community tool sharing platform** with a .NET 9 Web API backend and Blazor WebAssembly frontend. The backend follows **Clean Architecture** with three layers:

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
- **Payment** → processes financial transactions for rentals
- **Dispute** → handles rental conflicts with evidence and communication
- **FraudCheck** → monitors suspicious activity patterns
- All entities have audit fields (CreatedAt, UpdatedAt) and soft deletion

### Data Access Pattern
- **Repository Pattern**: Generic `IRepository<T>` with `Repository<T>` implementation
- **Unit of Work**: `IUnitOfWork` for transaction management
- **Mapster**: Object mapping between entities and DTOs (converted from records to classes to fix mapping issues)
- **CQRS Pattern**: Commands for writes, Queries for reads using record types

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
- **Handlers**: MediatR pattern with separate handler classes

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

### Email Notification Types
The system supports professional email templates for:
- **Authentication**: Welcome, verification, password reset, security alerts
- **Rentals**: Requests, approvals, rejections, reminders, overdue notifications
- **Payments**: Processing confirmations, receipts, payout notifications, failed payments
- **Disputes**: Creation, messages, status changes, escalation, resolution, evidence upload
- **Security**: Login alerts, two-factor codes, suspicious activity warnings
- **System**: Maintenance notifications, terms updates, privacy policy changes

### File Storage Service
- **`IFileStorageService`** - Universal file management
  - Location: `ToolsSharing.Infrastructure.Services.LocalFileStorageService`
  - Used by dispute evidence, tool images, and user uploads
  - Configurable validation rules and storage providers
  - Security features: file type validation, size limits, secure paths

### Usage Examples

#### Payment Processing
```csharp
// Inject payment service
[Inject] private IPaymentService PaymentService { get; set; }

// Process rental payment
var result = await PaymentService.ProcessRentalPaymentAsync(new ProcessRentalPaymentRequest
{
    RentalId = rentalId,
    PaymentMethod = PaymentMethod.PayPal,
    ReturnUrl = "https://localhost:5001/payment/success",
    CancelUrl = "https://localhost:5001/payment/cancel"
});
```

#### Dispute Creation
```csharp
// Inject dispute service
[Inject] private IDisputeService DisputeService { get; set; }

// Create dispute with evidence
var result = await DisputeService.CreateDisputeAsync(new CreateDisputeRequest
{
    RentalId = rentalId,
    Type = DisputeType.ToolDamage,
    Category = DisputeCategory.Quality,
    Title = "Tool was damaged upon pickup",
    Description = "The drill had a broken chuck...",
    Evidence = evidenceFiles
});
```

#### File Upload
```csharp
// Inject file storage service
[Inject] private IFileStorageService FileStorage { get; set; }

// Upload file with validation
var storagePath = await FileStorage.UploadFileAsync(
    fileStream, 
    fileName, 
    contentType, 
    folder: "disputes/evidence"
);
```

#### Email Notifications
```csharp
// Inject email service
[Inject] private IEmailNotificationService EmailService { get; set; }

// Send notification
var notification = new DisputeCreatedNotification
{
    RecipientEmail = user.Email,
    RecipientName = user.Name,
    DisputeTitle = dispute.Title,
    // ... other properties
};
await EmailService.SendNotificationAsync(notification);
```

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

**Recently Completed**:
- ✅ .NET 9 upgrade (completed January 2025)
  - All projects upgraded from .NET 8 to .NET 9
  - WSL compatibility issues resolved with Directory.Build.props
  - Docker images updated to .NET 9
  - All documentation and scripts updated
- ✅ Frontend compilation fixes (completed January 2025)
  - Fixed Tool.Images property access in OverdueRentalsManagement.razor (changed to ImageUrls)
  - Resolved Guid conversion errors in admin components
  - Fixed MudBlazor 8.x filter lambda expression issues
  - Resolved nullable reference warnings in ExtendRentalDialog.razor
- ✅ AutoMapper to Mapster migration
  - Resolved commercial licensing concerns
  - Improved performance and maintained functionality
  - Updated service registrations and configurations
- ✅ MudBlazor 8.9.0 upgrade  
  - Updated from 7.15.0 to 8.9.0 for better .NET 9 compatibility
  - Fixed breaking changes in DialogOptions and dialog API
  - Updated date picker event handling to use @bind-Date:after
- ✅ Authentication state consistency fixes
  - Enhanced JWT token validation in CustomAuthenticationStateProvider
  - Automatic cleanup of inconsistent auth data
  - Improved 401 response handling with auto-logout
- ✅ Dialog component fixes
  - Fixed rental dialog close/cancel button functionality
  - Updated DialogParameters to use non-generic approach for MudBlazor 8.x
  - Resolved MissingMethodException in dialog creation
- ✅ Admin Dashboard Implementation
  - Added role-based authentication with Admin role
  - JWT tokens include role claims and IsAdmin flag
  - Admin dashboard with comprehensive overview and statistics
  - Conditional navigation menu items for admin users
  - John Doe is seeded as admin user for testing
- ✅ Production-Ready Admin Management System (completed January 2025)
  - **FraudManagement.razor** - Complete fraud detection center with real-time alerts, risk level filtering, and admin workflows
  - **UserManagement.razor** - Comprehensive user administration with search, suspend/unsuspend, verification, and activity tracking
  - **PaymentManagement.razor** - Payment oversight with status management, approval/rejection workflows, retry/refund capabilities
  - **DisputeManagement.razor** - Full dispute resolution center with filtering, escalation to PayPal, and resolution workflows
  - **Real Backend Integration** - All admin pages use actual API endpoints instead of mockup data
  - **Advanced Filtering** - Comprehensive search and filtering capabilities across all management interfaces
  - **Role-Based Security** - All admin pages protected with `[Authorize(Roles = "Admin")]`
  - **Professional UI** - Modern MudBlazor 8.x components with responsive design and consistent styling
- ✅ Comprehensive Payment System (completed January 2025)
  - PayPal integration with secure webhook validation
  - Commission calculation and automated owner payouts
  - Security deposit handling with real PayPal refunds
  - Advanced fraud detection and velocity limits
  - Professional receipt generation and tracking
  - Payment status communication and timeline explanations
- ✅ Dispute Management System (completed January 2025)
  - Full dispute workflow with evidence upload capability
  - File storage service with security validation
  - Communication system between parties and admins
  - PayPal dispute API integration for escalation
  - Comprehensive email notification system
  - Professional HTML email templates for all dispute events
- ✅ Advanced Services Architecture (completed January 2025)
  - Universal email notification service with 15+ template types
  - File storage abstraction with local/cloud provider support
  - Payment receipt service with detailed breakdowns
  - Background services for automated payout processing
  - Fraud detection service with configurable rules
- ✅ Comprehensive Rental Workflow System (completed January 2025)
  - Pickup/return confirmation API endpoints with PATCH /api/rentals/{id}/pickup and /api/rentals/{id}/return
  - RentalLifecycleService background service for automated rental state transitions and overdue detection
  - Enhanced RentalDetails.razor with 'Confirm Pickup', 'Confirm Return', and 'Extend Rental' buttons
  - Progressive return reminder email notifications (2 days before, 1 day before, same day, and overdue escalation)
  - Overdue rental detection with 1-day, 3-day, 7-day, and weekly notification escalation
  - Rental extension functionality with conflict detection and additional cost calculation
  - Admin overdue rental management UI with comprehensive filtering and actions
  - Mobile push notification infrastructure with device token management (placeholder implementation)
  - SMS notification infrastructure with Twilio/AWS SNS integration points (placeholder implementation)
- ✅ Overdue Rental UI Alert System (completed January 2025)
  - OverdueRentalAlertComponent.razor - Comprehensive overdue alert component with 295 lines of code
  - Progressive severity levels based on days overdue (Recent -> Moderate -> Severe -> Critical)
  - Integrated alerts on Home.razor, MyRentals.razor, and MyTools.razor pages
  - Auto-refresh functionality every 5 minutes with Timer-based updates
  - Differentiated alerts for renters (need to return) vs owners (tools are overdue)
  - Action buttons for quick resolution (Mark as Returned, Contact Renter, View Details)
  - Dismissible alerts with local state management for user experience
  - Professional styling with CSS classes and progress indicators
  - Real-time overdue progress calculation with maximum 14-day scale
- ✅ Comprehensive Messaging System (completed January 2025)
  - Complete database schema with Messages, Conversations, MessageAttachments tables and proper foreign key relationships
  - RESTful API endpoints via ConversationsController and MessagesController with full CRUD operations
  - Modern frontend interface (Messages.razor) with conversations/messages tabs, search functionality, and filtering
  - Message statistics dashboard with real-time counts for total, unread, sent messages, and conversation counts
  - Last message preview in conversations list with participant names, formatted timestamps, and truncated content
  - Advanced filtering system with Read/Unread/All options and intelligent defaults (All selected by default)
  - Database migration fixes resolving MessageAttachments schema inconsistencies and improved data seeding logic
  - DTO consistency fixes standardizing property mapping between backend (LastMessageContent) and frontend DTOs
  - MudBlazor 8.x compatibility fixes resolving component type parameters and property binding issues
  - Professional UI design with modern conversation list, user avatars, unread indicators, and responsive layout
  - Comprehensive backend service integration with real API endpoints replacing all mockup data
  - Messaging integration across Tool Details, User Profile, and Rental Details pages with proper context linking
  - NewMessageDialog with proper recipient population, tool/rental context tags, and file attachment support
- ✅ Rental Data Transfer Object Enhancement (completed January 2025)
  - Fixed missing Tool property in backend RentalDto class preventing frontend from accessing complete tool information
  - Enhanced Mapster mapping configuration to include full Tool object in rental API responses
  - Resolved "Tool Information" section display issues in RentalDetails.razor by ensuring complete tool data availability
  - Improved message dialog context tags with meaningful tool names instead of truncated GUIDs
  - Backend-to-frontend data flow optimization ensuring rental endpoints return complete tool details (name, description, category, brand, model, rates, condition, location, images)
  - Verified fix through API testing with proper JWT authentication and confirmed Tool object inclusion in /api/rentals and /api/rentals/{id} responses

See `TODO_MASTER_INDEX.md` for detailed timelines and resource recommendations.

## Important Development Notes

### Recent Architecture Changes
- **Mapster Configuration**: All object mapping now uses `TypeAdapterConfig` instead of AutoMapper profiles
- **MudBlazor 8.x Compatibility**: Dialog creation uses simplified `DialogParameters` and `DialogOptions`
- **WSL Development**: Universal compatibility via `Directory.Build.props` without hardcoded paths
- **Authentication Robustness**: Enhanced token validation prevents inconsistent auth states
- **Service Layer Expansion**: Comprehensive service architecture with payment, dispute, email, and file storage services
- **Email Notification System**: Professional email templates with user preference management and delivery tracking

### Known Working Patterns
- **Dialog Creation**: Use `new DialogParameters()` and `.Add()` method instead of generic syntax
- **Date Picker Events**: Use `@bind-Date:after="Method"` instead of `OnDateChanged`
- **Authentication**: State automatically syncs between localStorage/sessionStorage and auth provider
- **WSL Builds**: All .NET commands work universally without path modifications
- **Service Injection**: All services are registered in DI and available via `[Inject]` in Blazor or constructor injection in backend
- **Email Notifications**: Use specific notification classes (e.g., `DisputeCreatedNotification`) rather than generic email sending
- **File Uploads**: Always use `IFileStorageService` for consistent validation and storage abstraction
- **Payment Processing**: All payment operations go through `IPaymentService` with automatic fraud detection and receipt generation
- **DTO Design**: Backend DTOs must be classes (not records) with nullable reference properties for Mapster compatibility
- **API Response Structure**: All endpoints include nested objects when frontend needs complete information (e.g., Tool object within RentalDto)
- **Mapster Configuration**: Use explicit `.Map(dest => dest.Property, src => src.Property)` for complex object mappings in `MappingProfile.cs`

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

### Service Registration Issues
- All services are automatically registered in `DependencyInjection.cs`
- Payment services require PayPal configuration in `appsettings.json`
- Email services require SMTP configuration for production use
- File storage services use local storage by default (configurable for cloud providers)

## Quick Service Reference

### Available Services for Dependency Injection

#### Core Business Services
- `IAuthService` - Authentication and user management
- `IToolsService` - Tool listing and management
- `IRentalsService` - Rental creation and management
- `IUserService` - User profile and settings
- `ISettingsService` - Application and user settings

#### Payment and Financial Services
- `IPaymentService` - Payment processing and management
- `IPaymentProvider` - PayPal integration (extensible for other providers)
- `IPaymentStatusService` - Payment status tracking and communication
- `IPaymentReceiptService` - Receipt generation and tracking
- `IFraudDetectionService` - Fraud prevention and monitoring
- `IPayPalWebhookValidator` - Webhook security validation

#### Dispute and Communication Services  
- `IDisputeService` - Dispute management and resolution
- `IDisputeNotificationService` - Dispute-specific email notifications
- `IEmailNotificationService` - Universal email notification system
- `IFileStorageService` - File upload and storage management

#### Messaging Services
- `IMessageService` - Message and conversation management
- `MessageService` (frontend) - Frontend service for API communication
- `ConversationsController` - RESTful API for conversation operations
- `MessagesController` - RESTful API for message operations

#### Infrastructure Services
- `IRepository<T>` - Generic repository pattern
- `IUnitOfWork` - Transaction management
- `IJwtTokenService` - JWT token generation and validation
- `IMapper` - Mapster object mapping

## Feature Implementation Workflow

When implementing new features or completing tasks, follow this workflow:

### Implementation Steps
1. Use the TodoWrite tool to plan the task if required
2. Use available search tools to understand the codebase and requirements
3. Implement the solution using all available tools
4. Verify the solution if possible with tests
5. Run lint and typecheck commands (e.g., `npm run lint`, `npm run typecheck`, `ruff`, etc.) if available
6. **NEVER commit changes unless explicitly asked by the user**

### 🔄 CRITICAL: Update TODO Documentation After Feature Completion

**MANDATORY FINAL STEP**: After successfully implementing any feature or completing a significant task, you MUST update the relevant TODO files to reflect the progress:

#### Required TODO Updates:
1. **`TODO_MASTER_INDEX.md`**:
   - Mark completed items as ✅ **COMPLETED**
   - Update priority rankings if dependencies are resolved
   - Add new items to "Recently Completed" section with detailed description
   - Adjust phase timelines based on completed work

2. **Specific TODO files** (e.g., `TODO_BASIC_COMMISSION_SYSTEM.md`):
   - Mark completed sections as ✅ **COMPLETED**
   - Update implementation status and progress
   - Note any deviations from original plan (e.g., PayPal instead of Stripe)
   - Document lessons learned or architectural decisions

3. **`CLAUDE.md`** (this file):
   - Update "Recently Completed" section with new achievements
   - Add new services to Quick Service Reference if applicable
   - Update Known Working Patterns with new implementations
   - Document any new architectural patterns or best practices

#### Example Update Pattern:
```markdown
### ✅ Recently Completed:
- ✅ [Feature Name] (completed [Date])
  - [Key achievement 1]
  - [Key achievement 2] 
  - [Technical implementation details]
  - [Architectural decisions made]
```

**Why This Matters**: Keeping TODO files updated ensures accurate project tracking, prevents duplicate work, and maintains clear development roadmap for future sessions.