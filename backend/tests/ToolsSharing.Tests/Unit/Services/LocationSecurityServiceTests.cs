using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Enums;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Tests.Unit.Services;

/// <summary>
/// Unit tests for LocationSecurityService
/// </summary>
public class LocationSecurityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<LocationSecurityService>> _loggerMock;
    private readonly LocationSecurityConfiguration _config;
    private readonly LocationSecurityService _service;

    public LocationSecurityServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Create logger mock
        _loggerMock = new Mock<ILogger<LocationSecurityService>>();

        // Create test configuration
        _config = new LocationSecurityConfiguration
        {
            MaxSearchesPerHour = 50,
            MaxSearchesPerTarget = 5,
            MinSearchIntervalSeconds = 10,
            EnableTriangulationDetection = true,
            TriangulationMinDistanceKm = 1.0m,
            TriangulationTimeWindowHours = 24,
            TriangulationMinSearchPoints = 3,
            LogAllSearches = true,
            SearchLogRetentionDays = 90
        };

        _service = new LocationSecurityService(_context, _cache, _loggerMock.Object, _config);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public void GetDistanceBand_Should_Return_Correct_Band_For_Various_Distances()
    {
        // Test VeryClose (< 0.5km)
        _service.GetDistanceBand(0.3m).Should().Be(DistanceBand.VeryClose);
        
        // Test Nearby (< 2km)
        _service.GetDistanceBand(1.5m).Should().Be(DistanceBand.Nearby);
        
        // Test Moderate (< 10km)
        _service.GetDistanceBand(7.5m).Should().Be(DistanceBand.Moderate);
        
        // Test Far (< 50km)
        _service.GetDistanceBand(35.0m).Should().Be(DistanceBand.Far);
        
        // Test VeryFar (50km+)
        _service.GetDistanceBand(75.0m).Should().Be(DistanceBand.VeryFar);
    }

    [Fact]
    public void GetFuzzedDistance_Should_Add_Random_Noise_To_Distance()
    {
        // Arrange
        var originalDistance = 10.0m;

        // Act
        var fuzzedDistance = _service.GetFuzzedDistance(originalDistance);

        // Assert
        fuzzedDistance.Should().NotBe(originalDistance);
        fuzzedDistance.Should().BeGreaterThan(0);
        // Should be within reasonable bounds (±20% of original)
        fuzzedDistance.Should().BeInRange(originalDistance * 0.8m, originalDistance * 1.2m);
    }

    [Theory]
    [InlineData(PrivacyLevel.Exact, 0.0001)]
    [InlineData(PrivacyLevel.District, 0.001)]
    [InlineData(PrivacyLevel.ZipCode, 0.01)]
    [InlineData(PrivacyLevel.Neighborhood, 0.1)]
    public void QuantizeLocation_Should_Apply_Correct_Grid_Size_For_Privacy_Levels(PrivacyLevel privacyLevel, decimal expectedGridSize)
    {
        // Arrange
        var lat = 40.7831m;
        var lng = -73.9712m;

        // Act
        var (quantizedLat, quantizedLng) = _service.QuantizeLocation(lat, lng, privacyLevel);

        // Assert
        quantizedLat.Should().NotBe(lat); // Should be quantized
        quantizedLng.Should().NotBe(lng); // Should be quantized
        
        // Verify that the coordinates are properly snapped to grid centers
        // The algorithm uses: Math.Floor(coord / gridSize) * gridSize + (gridSize / 2)
        var expectedLatBase = Math.Floor(lat / expectedGridSize) * expectedGridSize;
        var expectedLngBase = Math.Floor(lng / expectedGridSize) * expectedGridSize;
        var expectedQuantizedLat = expectedLatBase + (expectedGridSize / 2);
        var expectedQuantizedLng = expectedLngBase + (expectedGridSize / 2);
        
        quantizedLat.Should().Be(expectedQuantizedLat);
        quantizedLng.Should().Be(expectedQuantizedLng);
    }

    [Fact]
    public void GetJitteredLocation_Should_Return_Consistent_Results_Within_Same_Hour()
    {
        // Arrange
        var lat = 40.7831m;
        var lng = -73.9712m;
        var privacyLevel = PrivacyLevel.District;

        // Act - Call multiple times within same hour
        var (jitteredLat1, jitteredLng1) = _service.GetJitteredLocation(lat, lng, privacyLevel);
        var (jitteredLat2, jitteredLng2) = _service.GetJitteredLocation(lat, lng, privacyLevel);

        // Assert - Should be the same due to time-based seed
        jitteredLat1.Should().Be(jitteredLat2);
        jitteredLng1.Should().Be(jitteredLng2);
        
        // But should be different from original
        jitteredLat1.Should().NotBe(lat);
        jitteredLng1.Should().NotBe(lng);
    }

    [Fact]
    public async Task LogLocationSearchAsync_Should_Create_Search_Log_Entry()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        var searchType = LocationSearchType.ToolSearch;
        var searchLat = 40.7831m;
        var searchLng = -73.9712m;
        var searchQuery = "New York";
        var userAgent = "TestAgent/1.0";
        var ipAddress = "192.168.1.1";

        // Act
        var result = await _service.LogLocationSearchAsync(userId, targetId, searchType, searchLat, searchLng, searchQuery, userAgent, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TargetId.Should().Be(targetId);
        result.SearchType.Should().Be(searchType);
        result.SearchLat.Should().Be(searchLat);
        result.SearchLng.Should().Be(searchLng);
        result.SearchQuery.Should().Be(searchQuery);
        result.UserAgent.Should().Be(userAgent);
        result.IpAddress.Should().Be(ipAddress);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it was saved to database
        var savedLog = await _context.LocationSearchLogs.FirstOrDefaultAsync(x => x.Id == result.Id);
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ValidateLocationSearchAsync_Should_Allow_Search_Within_Limits()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";

        // Act
        var result = await _service.ValidateLocationSearchAsync(userId, targetId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateLocationSearchAsync_Should_Block_Search_When_Hourly_Limit_Exceeded()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        
        // Create many search logs to exceed limit
        var searchLogs = new List<LocationSearchLog>();
        for (int i = 0; i < _config.MaxSearchesPerHour + 1; i++)
        {
            searchLogs.Add(new LocationSearchLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = $"target{i}",
                SearchType = LocationSearchType.ToolSearch,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30) // Within last hour
            });
        }
        
        await _context.LocationSearchLogs.AddRangeAsync(searchLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateLocationSearchAsync(userId, targetId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateLocationSearchAsync_Should_Block_Search_When_Target_Limit_Exceeded()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        
        // Create many search logs for same target to exceed limit
        var searchLogs = new List<LocationSearchLog>();
        for (int i = 0; i < _config.MaxSearchesPerTarget + 1; i++)
        {
            searchLogs.Add(new LocationSearchLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = targetId,
                SearchType = LocationSearchType.ToolSearch,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30) // Within last hour
            });
        }
        
        await _context.LocationSearchLogs.AddRangeAsync(searchLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateLocationSearchAsync(userId, targetId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTriangulationAttemptAsync_Should_Return_False_When_Detection_Disabled()
    {
        // Arrange
        var configWithDisabledDetection = new LocationSecurityConfiguration
        {
            EnableTriangulationDetection = false
        };
        var serviceWithDisabledDetection = new LocationSecurityService(_context, _cache, _loggerMock.Object, configWithDisabledDetection);
        
        var userId = "user123";
        var targetId = "target456";
        var searchType = LocationSearchType.ToolSearch;
        var searchLat = 40.7831m;
        var searchLng = -73.9712m;

        // Act
        var result = await serviceWithDisabledDetection.IsTriangulationAttemptAsync(userId, targetId, searchType, searchLat, searchLng, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTriangulationAttemptAsync_Should_Return_False_When_Not_Enough_Search_Points()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        var searchType = LocationSearchType.ToolSearch;
        
        // Add only 1 search point (need 3 for triangulation)
        var searchLog = new LocationSearchLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            SearchType = searchType,
            SearchLat = 40.7831m,
            SearchLng = -73.9712m,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        
        await _context.LocationSearchLogs.AddAsync(searchLog);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsTriangulationAttemptAsync(userId, targetId, searchType, 40.7900m, -73.9800m, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTriangulationAttemptAsync_Should_Detect_Triangular_Pattern()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        var searchType = LocationSearchType.ToolSearch;
        
        // Create a triangular search pattern around a target location
        var searchLogs = new List<LocationSearchLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = targetId,
                SearchType = searchType,
                SearchLat = 40.7831m, // Point 1
                SearchLng = -73.9712m,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = targetId,
                SearchType = searchType,
                SearchLat = 40.7900m, // Point 2 (forms triangle)
                SearchLng = -73.9600m,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };
        
        await _context.LocationSearchLogs.AddRangeAsync(searchLogs);
        await _context.SaveChangesAsync();

        // Act - Add third point to complete triangle
        var result = await _service.IsTriangulationAttemptAsync(userId, targetId, searchType, 40.7750m, -73.9800m, null);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(DistanceBand.VeryClose, "Very close (< 0.5 km)")]
    [InlineData(DistanceBand.Nearby, "Nearby (< 2 km)")]
    [InlineData(DistanceBand.Moderate, "Moderate distance (< 10 km)")]
    [InlineData(DistanceBand.Far, "Far (< 50 km)")]
    [InlineData(DistanceBand.VeryFar, "Very far (50+ km)")]
    public void GetDistanceBandText_Should_Return_Correct_Text_For_Each_Band(DistanceBand band, string expectedText)
    {
        // Act
        var result = _service.GetDistanceBandText(band);

        // Assert
        result.Should().Be(expectedText);
    }

    [Fact]
    public async Task LogLocationSearchAsync_Should_Mark_As_Suspicious_When_Triangulation_Detected()
    {
        // Arrange
        var userId = "user123";
        var targetId = "target456";
        var searchType = LocationSearchType.ToolSearch;
        
        // Create existing searches that would trigger triangulation detection
        var searchLogs = new List<LocationSearchLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = targetId,
                SearchType = searchType,
                SearchLat = 40.7831m,
                SearchLng = -73.9712m,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetId = targetId,
                SearchType = searchType,
                SearchLat = 40.7900m,
                SearchLng = -73.9600m,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };
        
        await _context.LocationSearchLogs.AddRangeAsync(searchLogs);
        await _context.SaveChangesAsync();

        // Act - Log a search that completes the triangular pattern
        var result = await _service.LogLocationSearchAsync(userId, targetId, searchType, 40.7750m, -73.9800m, null, null, null);

        // Assert
        result.IsSuspicious.Should().BeTrue();
        result.SuspiciousReason.Should().Contain("triangulation");
    }

    [Fact]
    public async Task ValidateLocationSearchAsync_Should_Allow_Anonymous_Searches()
    {
        // Act
        var result = await _service.ValidateLocationSearchAsync(null, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetDistanceBand_Should_Handle_Edge_Cases()
    {
        // Test exact boundary values (boundaries are inclusive for the lower band)
        _service.GetDistanceBand(0.5m).Should().Be(DistanceBand.VeryClose); // <= 0.5m is VeryClose
        _service.GetDistanceBand(2.0m).Should().Be(DistanceBand.Nearby);     // <= 2.0m is Nearby
        _service.GetDistanceBand(10.0m).Should().Be(DistanceBand.Moderate);  // <= 10.0m is Moderate
        _service.GetDistanceBand(50.0m).Should().Be(DistanceBand.Far);       // <= 50.0m is Far
        
        // Test zero distance
        _service.GetDistanceBand(0.0m).Should().Be(DistanceBand.VeryClose);
    }

    [Fact]
    public void GetFuzzedDistance_Should_Never_Return_Negative_Distance()
    {
        // Arrange
        var smallDistance = 0.1m;

        // Act - Run multiple times to test randomness
        for (int i = 0; i < 100; i++)
        {
            var fuzzedDistance = _service.GetFuzzedDistance(smallDistance);
            
            // Assert
            fuzzedDistance.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Fact]
    public void QuantizeLocation_Should_Handle_Negative_Coordinates()
    {
        // Arrange
        var lat = -40.7831m; // Southern hemisphere
        var lng = -73.9712m; // Western hemisphere
        var privacyLevel = PrivacyLevel.District;

        // Act
        var (quantizedLat, quantizedLng) = _service.QuantizeLocation(lat, lng, privacyLevel);

        // Assert
        quantizedLat.Should().BeLessThan(lat); // Should be quantized
        quantizedLng.Should().BeLessThan(lng); // Should be quantized
        quantizedLat.Should().BeNegative();
        quantizedLng.Should().BeNegative();
    }
}