# TODO: .NET 9 Upgrade

## Overview
Upgrade the entire NeighborTools solution from .NET 8 to .NET 9 to take advantage of performance improvements, new features, and enhanced security.

## Current State
- All projects currently target `net8.0`
- Using .NET 8 packages and framework versions
- Some package warnings about version mismatches (e.g., DependencyInjection.Abstractions)

## Benefits of .NET 9
- **Performance Improvements**: Better runtime performance and reduced memory usage
- **New Language Features**: C# 13 features and enhancements
- **Enhanced Security**: Latest security patches and improvements
- **Better Tooling**: Improved debugging and diagnostics
- **Updated Dependencies**: Access to latest package versions

## Upgrade Tasks

### 1. Backend Projects (.NET 8 → .NET 9)

#### Project Files Update
- [ ] `backend/src/ToolsSharing.API/ToolsSharing.API.csproj`
  - Update `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net9.0</TargetFramework>`
- [ ] `backend/src/ToolsSharing.Core/ToolsSharing.Core.csproj`
  - Update `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net9.0</TargetFramework>`
- [ ] `backend/src/ToolsSharing.Infrastructure/ToolsSharing.Infrastructure.csproj`
  - Update `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net9.0</TargetFramework>`
- [ ] `backend/tests/ToolsSharing.Tests/ToolsSharing.Tests.csproj`
  - Update `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net9.0</TargetFramework>`

#### Package Updates
- [ ] Update Entity Framework Core packages to 9.x versions:
  - `Microsoft.EntityFrameworkCore.Design`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Pomelo.EntityFrameworkCore.MySql`
- [ ] Update ASP.NET Core packages to 9.x versions:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Update other Microsoft packages:
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
  - `Microsoft.Extensions.Hosting`
  - `Microsoft.Extensions.Logging`

### 2. Frontend Project (.NET 8 → .NET 9)

#### Project File Update
- [ ] `frontend/frontend.csproj`
  - Update `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net9.0</TargetFramework>`

#### Package Updates
- [ ] Update Blazor packages to 9.x versions:
  - `Microsoft.AspNetCore.Components.WebAssembly`
  - `Microsoft.AspNetCore.Components.WebAssembly.DevServer`
  - `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- [ ] Update other packages:
  - `Microsoft.Extensions.Http`
  - `System.Net.Http.Json`

### 3. Docker Configuration Updates

#### Dockerfile Updates
- [ ] `backend/docker/Dockerfile`
  - Update base image: `mcr.microsoft.com/dotnet/aspnet:8.0` → `mcr.microsoft.com/dotnet/aspnet:9.0`
  - Update SDK image: `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/sdk:9.0`

### 4. Documentation Updates

#### README Files
- [ ] Update `README.md` - Change .NET 8 references to .NET 9
- [ ] Update `backend/README.md` - Update technology stack section
- [ ] Update `frontend/README.md` - Update dependencies section
- [ ] Update `CLAUDE.md` - Update project architecture section

#### Installation Scripts
- [ ] `backend/scripts/install.sh`
  - Update prerequisite check from ".NET 8 SDK" to ".NET 9 SDK"
  - Update download link reference
- [ ] Update other script documentation that mentions .NET 8

### 5. Configuration and Settings

#### Launch Settings
- [ ] Verify `launchSettings.json` files work with .NET 9
- [ ] Test hot reload functionality
- [ ] Verify debugging configuration

#### Development Environment
- [ ] Test Docker containers with .NET 9 runtime
- [ ] Verify development scripts work with .NET 9
- [ ] Test both local and containerized development modes

### 6. Testing and Validation

#### Build Verification
- [ ] `dotnet build` - Ensure solution builds without errors
- [ ] `dotnet test` - Run all tests to ensure compatibility
- [ ] `dotnet restore` - Verify all packages restore correctly

#### Runtime Testing
- [ ] Test API endpoints functionality
- [ ] Test frontend authentication flow
- [ ] Test database operations and migrations
- [ ] Test Docker container startup and operation

#### Performance Validation
- [ ] Compare startup times (before/after)
- [ ] Test memory usage patterns
- [ ] Verify no performance regressions

### 7. Deployment Considerations

#### Environment Updates
- [ ] Update development environment to .NET 9 SDK
- [ ] Update CI/CD pipelines to use .NET 9
- [ ] Update production deployment configurations

#### Compatibility Checks
- [ ] Verify MySQL driver compatibility with .NET 9
- [ ] Test Redis client compatibility
- [ ] Validate third-party package compatibility

## Implementation Steps

### Phase 1: Development Environment
1. Install .NET 9 SDK
2. Update backend projects and packages
3. Test backend functionality

### Phase 2: Frontend Update
1. Update frontend project and packages
2. Test frontend functionality
3. Test frontend-backend integration

### Phase 3: Infrastructure
1. Update Docker configurations
2. Test containerized deployment
3. Update development scripts

### Phase 4: Documentation and Testing
1. Update all documentation
2. Comprehensive testing
3. Performance validation

## Potential Issues and Mitigation

### Breaking Changes
- **Risk**: Some packages may have breaking changes in v9
- **Mitigation**: Test thoroughly and check package changelogs

### Package Compatibility
- **Risk**: Third-party packages may not support .NET 9 immediately
- **Mitigation**: Check package compatibility before upgrading

### Docker Image Availability
- **Risk**: .NET 9 Docker images may have different configurations
- **Mitigation**: Test Docker builds and runtime behavior

### Performance Impact
- **Risk**: Potential performance regressions (unlikely but possible)
- **Mitigation**: Performance testing and monitoring

## Success Criteria

- [ ] All projects build successfully with .NET 9
- [ ] All existing functionality works as expected
- [ ] No performance regressions
- [ ] All tests pass
- [ ] Docker containers work correctly
- [ ] Development workflow remains smooth
- [ ] Documentation is updated and accurate

## Post-Upgrade Benefits

### Immediate Benefits
- Latest security patches
- Performance improvements
- Access to C# 13 features
- Updated tooling and diagnostics

### Long-term Benefits
- Longer support lifecycle for .NET 9
- Foundation for future .NET updates
- Better ecosystem compatibility
- Enhanced development experience

## Timeline Estimate

- **Planning and Preparation**: 1 day
- **Backend Upgrade**: 2-3 days
- **Frontend Upgrade**: 1-2 days
- **Testing and Validation**: 2-3 days
- **Documentation Updates**: 1 day

**Total Estimated Time**: 7-10 days

## Dependencies

- .NET 9 SDK availability
- Package compatibility verification
- Testing environment setup
- Team coordination for testing

---

**Note**: This upgrade should be done in a feature branch with thorough testing before merging to main. Consider creating a backup of the current working state before beginning the upgrade process.