# NeighborTools Frontend

A modern Blazor WebAssembly frontend for the NeighborTools community tool sharing platform.

## 🚀 Quick Start

```bash
cd frontend
dotnet run
```

Access the application at: http://localhost:5000

## 📖 Overview

The frontend is built with Blazor WebAssembly, providing a responsive single-page application experience with:

- **Component-based architecture** with reusable UI components
- **Automatic authentication** state management with JWT tokens
- **Real-time API communication** with the backend
- **Responsive design** using Bootstrap
- **Local storage integration** for authentication persistence

## 🏗️ Architecture

### Key Components

**Pages:**
- `Home.razor` - Landing page with featured tools
- `Tools.razor` - Browse all available tools
- `MyTools.razor` - Manage user's own tools
- `ToolDetails.razor` - Detailed tool view with rental options
- `Login.razor` / `Register.razor` - Authentication pages
- `MyRentals.razor` - User's rental history
- `Profile.razor` - User profile management

**Services:**
- `AuthService.cs` - Authentication and user management
- `ToolService.cs` - Tool-related API operations
- `RentalService.cs` - Rental management
- `AuthenticatedHttpClientHandler.cs` - Automatic JWT token injection
- `CustomAuthenticationStateProvider.cs` - Authentication state management
- `LocalStorageService.cs` - Browser storage operations

**Layout:**
- `MainLayout.razor` - Main application layout with header and navigation
- `RedirectToLogin.razor` - Authentication guard component

## 🔧 Configuration

### API Connection

The frontend automatically detects the backend API URL based on environment:

- **Development**: http://localhost:5000 (when using dotnet run) or http://localhost:5002 (when using Docker)
- **Production**: Uses the deployment URL

### Authentication

JWT tokens are automatically:
- Stored in browser localStorage
- Attached to all API requests via `AuthenticatedHttpClientHandler`
- Refreshed when expired
- Removed on logout

## 🛠️ Development

### Prerequisites
- .NET 8 SDK
- Backend API running (see backend/README.md)

### Running the Application

```bash
# Development mode
dotnet run

# Development with hot reload
dotnet watch run

# Build for production
dotnet build --configuration Release
```

### Project Structure

```
frontend/
├── Pages/                  # Razor pages/components
│   ├── Home.razor         # Landing page
│   ├── Tools.razor        # Tool browsing
│   ├── MyTools.razor      # User's tools
│   ├── Login.razor        # Authentication
│   └── ...
├── Services/              # API communication
│   ├── AuthService.cs     # Authentication
│   ├── ToolService.cs     # Tool operations
│   ├── RentalService.cs   # Rental operations
│   └── ...
├── Layout/                # Layout components
│   ├── MainLayout.razor   # Main layout
│   └── ...
├── Models/                # Frontend models
│   ├── AuthModels.cs      # Authentication DTOs
│   ├── ToolModels.cs      # Tool DTOs
│   └── ...
├── Shared/                # Shared components
│   ├── RedirectToLogin.razor
│   └── RentalRequestDialog.razor
├── wwwroot/               # Static assets
│   ├── css/              # Stylesheets
│   ├── index.html        # App entry point
│   └── ...
└── Program.cs             # Application configuration
```

## 🔒 Security Features

### Authentication Flow
1. User logs in via `Login.razor`
2. JWT token received and stored in localStorage
3. `CustomAuthenticationStateProvider` manages auth state
4. `AuthenticatedHttpClientHandler` adds token to all API requests
5. Automatic token refresh on expiration

### Protected Routes
- Routes requiring authentication automatically redirect to login
- Authentication state is preserved across browser sessions
- Tokens are validated on application startup

## 🎨 UI Components

### Reusable Components
- `RentalRequestDialog.razor` - Modal for rental requests
- `RedirectToLogin.razor` - Authentication guard
- Navigation components with role-based visibility

### Styling
- **Bootstrap 5** for responsive design
- **Custom CSS** for NeighborTools branding
- **Component-scoped styles** using `.razor.css` files

## 🌐 API Integration

### HTTP Client Configuration
```csharp
// Program.cs
builder.Services.AddScoped<AuthenticatedHttpClientHandler>();
builder.Services.AddHttpClient("api", client => {
    client.BaseAddress = new Uri("http://localhost:5000/");
}).AddHttpMessageHandler<AuthenticatedHttpClientHandler>();
```

### Service Pattern
All API communication follows a consistent service pattern:
- Services inject named HttpClient
- DTOs for request/response models
- Consistent error handling
- Automatic authentication via message handler

## 📱 Features

### Tool Management
- **Browse Tools** - Grid view with search and filtering
- **Tool Details** - Comprehensive tool information with images
- **My Tools** - Manage personal tool listings
- **Create/Edit Tools** - Form-based tool management

### Rental System
- **Rental Requests** - Submit rental requests with date ranges
- **Rental History** - View past and current rentals
- **Approval Workflow** - Tool owners can approve/reject requests

### User Experience
- **Responsive Design** - Works on desktop and mobile
- **Real-time Updates** - Immediate feedback on actions
- **Form Validation** - Client-side validation with server backup
- **Loading States** - User feedback during API operations

## 🐛 Troubleshooting

### Common Issues

1. **API Connection Failed**
   ```bash
   # Check if backend is running
   curl http://localhost:5000/health
   
   # Or if using Docker backend
   curl http://localhost:5002/health
   ```

2. **Authentication Issues**
   - Clear browser localStorage: `F12 > Application > Local Storage > Clear`
   - Check browser console for JWT errors
   - Verify backend authentication endpoints

3. **Build Errors**
   ```bash
   # Clean and rebuild
   dotnet clean
   dotnet restore
   dotnet build
   ```

4. **Hot Reload Issues**
   ```bash
   # Restart with clean
   dotnet clean
   dotnet watch run
   ```

### Development Tips

- Use browser Developer Tools for debugging
- Check browser console for errors
- Monitor Network tab for API request/response
- Use `@debug` directive in Razor components for debugging

## 📊 Performance

### Optimization Features
- **Lazy loading** for large components
- **Component disposal** for proper cleanup
- **Efficient API calls** with minimal data transfer
- **Local state management** to reduce API calls

### Bundle Size
- Minimal dependencies for smaller bundle
- Tree-shaking for unused code elimination
- Optimized build for production deployment

## 🚀 Deployment

### Build for Production
```bash
dotnet publish --configuration Release
```

### Static File Hosting
The output can be hosted on any static file server:
- **wwwroot** folder contains all necessary files
- **index.html** is the entry point
- Configure server for SPA routing

### Environment Configuration
Update API base URL in `Program.cs` for production deployment.

## 🤝 Contributing

1. Follow Blazor component conventions
2. Use consistent service patterns
3. Implement proper error handling
4. Add loading states for user feedback
5. Test authentication flows thoroughly

## 📝 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Components.WebAssembly | 8.0.x | Blazor WebAssembly framework |
| Microsoft.AspNetCore.Components.Authorization | 8.0.x | Authentication state management |
| System.Net.Http.Json | 8.0.x | HTTP JSON operations |
| Microsoft.Extensions.Http | 8.0.x | HttpClient factory |

All dependencies are included in the .NET 8 framework or explicitly defined in the project file.