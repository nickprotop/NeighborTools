using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Tests.Integration;

/// <summary>
/// Integration tests for Location Services including provider switching
/// </summary>
public class LocationServicesIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public LocationServicesIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Add logging
        services.AddLogging();
        
        // Create configuration with OpenStreetMap as default
        var configuration = CreateConfiguration(defaultProvider: "OpenStreetMap", hereApiKey: "");
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add infrastructure services (which includes location services)
        services.AddInfrastructure(configuration);
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _context.Dispose();
    }

    [Fact]
    public void LocationSecurityService_Should_Be_Registered()
    {
        // Act
        var service = _serviceProvider.GetService<ILocationSecurityService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<LocationSecurityService>();
    }

    [Fact]
    public void Primary_GeocodingService_Should_Default_To_OpenStreetMap_When_HERE_Not_Configured()
    {
        // Act
        var geocodingService = _serviceProvider.GetService<IGeocodingService>();

        // Assert
        geocodingService.Should().NotBeNull();
        geocodingService.Should().BeOfType<OpenStreetMapGeocodingService>();
        geocodingService!.ProviderName.Should().Be("OpenStreetMap");
    }

    [Fact]
    public void OpenStreetMapGeocodingService_Should_Always_Be_Available()
    {
        // Act
        var osmService = _serviceProvider.GetService<OpenStreetMapGeocodingService>();

        // Assert
        osmService.Should().NotBeNull();
        osmService.ProviderName.Should().Be("OpenStreetMap");
    }

    [Fact]
    public void HereGeocodingService_Should_Not_Be_Registered_Without_API_Key()
    {
        // Act
        var hereService = _serviceProvider.GetService<HereGeocodingService>();

        // Assert
        hereService.Should().BeNull(); // Should not be registered when API key is missing
    }

    [Fact]
    public void Provider_Switching_Should_Work_With_HERE_When_API_Key_Configured()
    {
        // Arrange - Create new service provider with HERE configured
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddMemoryCache();
        services.AddLogging();
        
        // Configuration with HERE as default and API key provided
        var configuration = CreateConfiguration(defaultProvider: "HERE", hereApiKey: "test-api-key-12345");
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        
        using var hereServiceProvider = services.BuildServiceProvider();

        // Act
        var primaryGeocodingService = hereServiceProvider.GetService<IGeocodingService>();
        var hereService = hereServiceProvider.GetService<HereGeocodingService>();
        var osmService = hereServiceProvider.GetService<OpenStreetMapGeocodingService>();

        // Assert
        primaryGeocodingService.Should().NotBeNull();
        primaryGeocodingService.Should().BeOfType<HereGeocodingService>();
        primaryGeocodingService!.ProviderName.Should().Be("HERE");
        
        hereService.Should().NotBeNull();
        osmService.Should().NotBeNull(); // OpenStreetMap should always be available as fallback
    }

    [Fact]
    public void Provider_Switching_Should_Fallback_To_OpenStreetMap_When_HERE_Requested_But_Not_Configured()
    {
        // Arrange - Create new service provider with HERE requested but no API key
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddMemoryCache();
        services.AddLogging();
        
        // Configuration with HERE as default but no API key (should fallback to OpenStreetMap)
        var configuration = CreateConfiguration(defaultProvider: "HERE", hereApiKey: "");
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        
        using var fallbackServiceProvider = services.BuildServiceProvider();

        // Act
        var geocodingService = fallbackServiceProvider.GetService<IGeocodingService>();

        // Assert
        geocodingService.Should().NotBeNull();
        geocodingService.Should().BeOfType<OpenStreetMapGeocodingService>();
        geocodingService!.ProviderName.Should().Be("OpenStreetMap");
    }

    [Fact]
    public void All_Configuration_Classes_Should_Be_Bound_Correctly()
    {
        // Act
        var geocodingConfig = _serviceProvider.GetService<IConfiguration>()?.GetSection("Geocoding").Get<GeocodingConfiguration>();
        var osmConfig = _serviceProvider.GetService<IConfiguration>()?.GetSection("Geocoding:OpenStreetMap").Get<OpenStreetMapConfiguration>();
        var hereConfig = _serviceProvider.GetService<IConfiguration>()?.GetSection("Geocoding:HERE").Get<HereConfiguration>();
        var securityConfig = _serviceProvider.GetService<IConfiguration>()?.GetSection("Geocoding:Security").Get<LocationSecurityConfiguration>();
        var cacheConfig = _serviceProvider.GetService<IConfiguration>()?.GetSection("Geocoding:Cache").Get<GeocodingCacheConfiguration>();

        // Assert
        geocodingConfig.Should().NotBeNull();
        geocodingConfig!.DefaultProvider.Should().Be("OpenStreetMap");
        
        osmConfig.Should().NotBeNull();
        osmConfig!.BaseUrl.Should().Be("https://nominatim.openstreetmap.org");
        
        hereConfig.Should().NotBeNull();
        hereConfig!.BaseUrl.Should().Be("https://geocode.search.hereapi.com/v1");
        
        securityConfig.Should().NotBeNull();
        securityConfig!.MaxSearchesPerHour.Should().Be(50);
        
        cacheConfig.Should().NotBeNull();
        cacheConfig!.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task LocationSecurityService_Should_Work_With_Database()
    {
        // Arrange
        var locationSecurityService = _serviceProvider.GetRequiredService<ILocationSecurityService>();
        var userId = "test-user-123";

        // Act - First validation should always pass for a new user
        var validationResult = await locationSecurityService.ValidateLocationSearchAsync(userId);
        
        // If validation fails, let's test logging anyway to see if that works
        var searchLog = await locationSecurityService.LogLocationSearchAsync(
            userId, 
            "target-123", 
            Core.Enums.LocationSearchType.ToolSearch, 
            40.7831m, 
            -73.9712m, 
            "New York", 
            "TestAgent/1.0", 
            "192.168.1.1");

        // Assert - The search log should always work
        searchLog.Should().NotBeNull();
        searchLog.UserId.Should().Be(userId);
        searchLog.SearchLat.Should().Be(40.7831m);
        searchLog.SearchLng.Should().Be(-73.9712m);
        searchLog.SearchQuery.Should().Be("New York");
        
        // Verify it was saved to database
        var savedLog = await _context.LocationSearchLogs.FirstOrDefaultAsync(x => x.Id == searchLog.Id);
        savedLog.Should().NotBeNull();
        
        // Validation should pass for first search by a user (no rate limits should apply)
        validationResult.Should().BeTrue("First search by a user should always be allowed");
    }

    [Theory]
    [InlineData("OpenStreetMap", "")]
    [InlineData("HERE", "test-api-key")]
    [InlineData("InvalidProvider", "")]
    public void DI_Container_Should_Handle_Different_Provider_Configurations(string defaultProvider, string hereApiKey)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddMemoryCache();
        services.AddLogging();
        
        var configuration = CreateConfiguration(defaultProvider, hereApiKey);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        
        using var testServiceProvider = services.BuildServiceProvider();

        // Act
        var geocodingService = testServiceProvider.GetService<IGeocodingService>();

        // Assert
        geocodingService.Should().NotBeNull();
        
        // Should always fall back to OpenStreetMap if provider is invalid or HERE is not configured
        if (defaultProvider == "HERE" && !string.IsNullOrEmpty(hereApiKey))
        {
            geocodingService!.ProviderName.Should().Be("HERE");
        }
        else
        {
            geocodingService!.ProviderName.Should().Be("OpenStreetMap");
        }
    }

    [Fact]
    public void Configuration_Should_Handle_Missing_Geocoding_Section()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddMemoryCache();
        services.AddLogging();
        
        // Configuration without Geocoding section
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SomeOtherSection:Value"] = "test"
            });
        var configuration = configBuilder.Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        
        using var testServiceProvider = services.BuildServiceProvider();

        // Act
        var geocodingService = testServiceProvider.GetService<IGeocodingService>();
        var locationSecurityService = testServiceProvider.GetService<ILocationSecurityService>();

        // Assert
        geocodingService.Should().NotBeNull();
        geocodingService!.ProviderName.Should().Be("OpenStreetMap"); // Should fallback to default
        
        locationSecurityService.Should().NotBeNull();
    }

    private static IConfiguration CreateConfiguration(string defaultProvider, string hereApiKey)
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Geocoding:DefaultProvider"] = defaultProvider,
                ["Geocoding:OpenStreetMap:BaseUrl"] = "https://nominatim.openstreetmap.org",
                ["Geocoding:OpenStreetMap:UserAgent"] = "NeighborTools/1.0",
                ["Geocoding:OpenStreetMap:RequestsPerSecond"] = "1",
                ["Geocoding:OpenStreetMap:CacheDurationHours"] = "24",
                ["Geocoding:OpenStreetMap:TimeoutSeconds"] = "30",
                ["Geocoding:OpenStreetMap:DefaultLanguage"] = "en",
                ["Geocoding:OpenStreetMap:ContactEmail"] = "",
                
                ["Geocoding:HERE:BaseUrl"] = "https://geocode.search.hereapi.com/v1",
                ["Geocoding:HERE:ApiKey"] = hereApiKey,
                ["Geocoding:HERE:RequestsPerSecond"] = "10",
                ["Geocoding:HERE:CacheDurationHours"] = "24",
                ["Geocoding:HERE:TimeoutSeconds"] = "30",
                ["Geocoding:HERE:DefaultLanguage"] = "en",
                ["Geocoding:HERE:MaxResults"] = "20",
                ["Geocoding:HERE:IncludeAddressDetails"] = "true",
                ["Geocoding:HERE:IncludeMapView"] = "true",
                
                ["Geocoding:Security:MaxSearchesPerHour"] = "50",
                ["Geocoding:Security:MaxSearchesPerTarget"] = "5",
                ["Geocoding:Security:MinSearchIntervalSeconds"] = "10",
                ["Geocoding:Security:EnableTriangulationDetection"] = "true",
                ["Geocoding:Security:TriangulationMinDistanceKm"] = "1.0",
                ["Geocoding:Security:TriangulationTimeWindowHours"] = "24",
                ["Geocoding:Security:TriangulationMinSearchPoints"] = "3",
                ["Geocoding:Security:LogAllSearches"] = "true",
                ["Geocoding:Security:SearchLogRetentionDays"] = "90",
                
                ["Geocoding:Cache:Enabled"] = "true",
                ["Geocoding:Cache:DefaultDurationHours"] = "24",
                ["Geocoding:Cache:PopularLocationDurationHours"] = "168",
                ["Geocoding:Cache:ErrorCacheDurationMinutes"] = "15",
                ["Geocoding:Cache:MaxCacheSize"] = "10000",
                ["Geocoding:Cache:KeyPrefix"] = "geocoding:"
            });
        
        return configBuilder.Build();
    }
}