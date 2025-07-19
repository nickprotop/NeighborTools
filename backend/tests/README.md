# NeighborTools Test Framework

## Overview

This directory contains the comprehensive test suite for the NeighborTools application. The test framework is designed to ensure quality and reliability as you approach your first production release.

## Test Structure

```
tests/
├── ToolsSharing.Tests/
│   ├── BasicTests/           # Environment and framework tests
│   │   └── EnvironmentTests.cs
│   ├── Unit/                 # Unit tests for business logic
│   │   └── Entities/         # Entity behavior tests
│   │       └── EntityTests.cs
│   ├── appsettings.Testing.json
│   ├── xunit.runner.json
│   └── ToolsSharing.Tests.csproj
└── README.md
```

## Test Categories

### 1. **Environment Tests** (`BasicTests/`)
- Framework functionality verification
- Basic language feature testing
- Async operations testing
- Exception handling validation

### 2. **Unit Tests** (`Unit/`)
- **Entity Tests**: Core business entity behavior
- **Service Tests**: Business logic validation (expandable)
- **Repository Tests**: Data access patterns (expandable)

### 3. **Integration Tests** (future)
- API endpoint testing
- Database integration
- Authentication workflows
- End-to-end scenarios

## Running Tests

### Quick Test Run
```bash
# From the backend directory
cd tests/ToolsSharing.Tests
dotnet test
```

### Using the Test Script
```bash
# From the backend directory
./scripts/run-tests.sh

# With coverage report
./scripts/run-tests.sh --coverage

# Performance tests only
./scripts/run-tests.sh --performance
```

### Advanced Test Commands
```bash
# Run specific test class
dotnet test --filter "ClassName=EntityTests"

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests and generate coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in parallel
dotnet test --parallel
```

## Test Framework Features

### 🧪 **Core Testing Stack**
- **xUnit 2.9.3** - Modern testing framework
- **FluentAssertions 7.1.0** - Readable assertion library
- **.NET 9** - Latest framework support

### 📊 **Code Coverage**
- Coverlet integration for coverage collection
- Support for HTML coverage reports
- CI/CD pipeline integration ready

### 🔧 **Development Tools**
- Consistent test configuration via `xunit.runner.json`
- Test-specific app settings
- Visual Studio and VS Code integration

## Current Test Status

✅ **Working Tests (19 tests passing)**:
- Basic framework functionality
- Entity initialization and behavior
- Property assignment validation
- Timestamp generation
- Guid uniqueness
- Collection initialization

## Expanding the Test Suite

### Adding New Tests

1. **Unit Tests**: Add to `Unit/` directory
   ```csharp
   public class NewServiceTests
   {
       [Fact]
       public void Service_Should_Do_Something()
       {
           // Arrange
           // Act  
           // Assert
       }
   }
   ```

2. **Integration Tests**: Create `Integration/` directory
   ```csharp
   public class NewControllerTests : IClassFixture<WebApplicationFactory<Program>>
   {
       // Integration test implementation
   }
   ```

### Test Patterns

#### Entity Tests
```csharp
[Fact]
public void Entity_Should_Initialize_With_Defaults()
{
    // Act
    var entity = new SomeEntity();
    
    // Assert
    entity.Id.Should().NotBe(Guid.Empty);
    entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

#### Service Tests
```csharp
[Fact]
public async Task Service_Should_Return_Expected_Result()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    var service = new MyService(mockDependency.Object);
    
    // Act
    var result = await service.DoSomethingAsync();
    
    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

### Test Configuration

#### Test Settings (`appsettings.Testing.json`)
- In-memory database configuration
- Test-specific JWT settings
- Mock email/payment providers
- Disabled external services

#### Runner Configuration (`xunit.runner.json`)
- Sequential test execution
- Detailed diagnostics
- Long-running test timeouts

## Production Readiness

### Current Status: ✅ Basic Test Framework Ready

**What's Implemented:**
- ✅ Test project structure
- ✅ Core dependencies
- ✅ Entity behavior tests
- ✅ Test runner script
- ✅ Environment validation

**Next Steps for Production:**
1. **Service Layer Tests**: Add comprehensive service testing
2. **API Integration Tests**: Test all endpoints
3. **Authentication Tests**: Verify JWT workflows
4. **Database Tests**: Ensure data integrity
5. **Performance Tests**: Load and stress testing
6. **CI/CD Integration**: Automated testing pipeline

### Quality Gates for Production

Before production release, ensure:
- [ ] Code coverage > 80%
- [ ] All critical paths tested
- [ ] Integration tests for all APIs
- [ ] Authentication and authorization tests
- [ ] Payment system tests
- [ ] Message moderation tests
- [ ] Database migration tests
- [ ] Performance benchmarks established

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Tests
  run: |
    cd backend
    ./scripts/run-tests.sh --coverage
    
- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    file: ./backend/tests/TestResults/coverage.cobertura.xml
```

### Local Development
```bash
# Pre-commit testing
./scripts/run-tests.sh

# Watch mode for TDD
dotnet watch test --project tests/ToolsSharing.Tests
```

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure all project references are correct
2. **Package Conflicts**: Check for version mismatches
3. **Test Timeouts**: Adjust `xunit.runner.json` settings
4. **Coverage Issues**: Verify Coverlet configuration

### Debug Mode
```bash
# Run single test with debugging
dotnet test --filter "TestName" --logger:"console;verbosity=detailed"

# Attach debugger
dotnet test --logger:"console;verbosity=detailed" --debug
```

## Contributing

When adding new features to the main application:

1. **Write tests first** (TDD approach)
2. **Test both success and failure scenarios**
3. **Include edge cases and boundary conditions**
4. **Update this README** with new test categories
5. **Ensure tests pass** before submitting PRs

---

**Test Framework Version**: 1.0.0  
**Last Updated**: January 2025  
**Framework**: .NET 9 with xUnit  
**Coverage**: Basic entity tests implemented