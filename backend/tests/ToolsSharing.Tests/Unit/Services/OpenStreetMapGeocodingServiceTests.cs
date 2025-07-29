using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Tests.Unit.Services;

/// <summary>
/// Unit tests for OpenStreetMapGeocodingService
/// </summary>
public class OpenStreetMapGeocodingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<OpenStreetMapGeocodingService>> _loggerMock;
    private readonly OpenStreetMapConfiguration _config;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public OpenStreetMapGeocodingServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Create logger mock
        _loggerMock = new Mock<ILogger<OpenStreetMapGeocodingService>>();

        // Create test configuration
        _config = new OpenStreetMapConfiguration
        {
            BaseUrl = "https://nominatim.openstreetmap.org",
            UserAgent = "TestAgent/1.0",
            RequestsPerSecond = 1,
            CacheDurationHours = 24,
            TimeoutSeconds = 30,
            DefaultLanguage = "en",
            ContactEmail = "test@example.com"
        };

        // Create HTTP client mock
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public void ProviderName_Should_Return_OpenStreetMap()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var providerName = service.ProviderName;

        // Assert
        providerName.Should().Be("OpenStreetMap");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Return_Empty_List_For_Empty_Query()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Return_Cached_Results_When_Available()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResults = new List<LocationOption>
        {
            new() { DisplayName = "Cached Location", Lat = 40.7831m, Lng = -73.9712m }
        };
        
        var cacheKey = "osm_search:New York:5:";
        _cache.Set(cacheKey, cachedResults);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DisplayName.Should().Be("Cached Location");
        
        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_Successful_API_Response()
    {
        // Arrange
        var nominatimResponse = new List<NominatimResult>
        {
            new()
            {
                DisplayName = "New York, NY, USA",
                Lat = "40.7831",
                Lon = "-73.9712",
                PlaceRank = 12,
                Address = new NominatimAddress
                {
                    City = "New York",
                    State = "New York",
                    Country = "United States",
                    CountryCode = "us"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(nominatimResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DisplayName.Should().Be("New York, NY, USA");
        result.First().Lat.Should().Be(40.7831m);
        result.First().Lng.Should().Be(-73.9712m);
        result.First().City.Should().Be("New York");
        result.First().State.Should().Be("New York");
        result.First().Country.Should().Be("United States");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_API_Error_Response()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("Invalid Query");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nominatim API error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_Invalid_JSON_Response()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Invalid JSON")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON parsing error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Include_Country_Code_In_Request_When_Provided()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 5, "US");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("countrycodes=us");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Respect_Limit_Parameter()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 10);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("limit=10");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Cap_Limit_At_50()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 100); // Request more than 50

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("limit=50"); // Should be capped at 50
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Return_Cached_Result_When_Available()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResult = new LocationOption
        {
            DisplayName = "Cached Location",
            Lat = 40.7831m,
            Lng = -73.9712m
        };
        
        var cacheKey = "osm_reverse:40.783100:-73.971200";
        _cache.Set(cacheKey, cachedResult);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7831m, -73.9712m);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Cached Location");
        
        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Handle_Successful_API_Response()
    {
        // Arrange
        var nominatimResult = new NominatimResult
        {
            DisplayName = "Empire State Building, New York, NY, USA",
            Lat = "40.7484",
            Lon = "-73.9857",
            Address = new NominatimAddress
            {
                Road = "5th Avenue",
                City = "New York",
                State = "New York",
                Country = "United States"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(nominatimResult);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7484m, -73.9857m);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Empire State Building, New York, NY, USA");
        result.Lat.Should().Be(40.7484m);
        result.Lng.Should().Be(-73.9857m);
        result.City.Should().Be("New York");
        result.State.Should().Be("New York");
        result.Country.Should().Be("United States");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Return_Null_For_API_Error()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.ReverseGeocodeAsync(0.0m, 0.0m);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPopularLocationsAsync_Should_Return_Cached_Results_When_Available()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResults = new List<LocationOption>
        {
            new() { DisplayName = "Popular Location 1" },
            new() { DisplayName = "Popular Location 2" }
        };
        
        var cacheKey = "osm_popular:10";
        _cache.Set(cacheKey, cachedResults);

        // Act
        var result = await service.GetPopularLocationsAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.DisplayName == "Popular Location 1");
        result.Should().Contain(x => x.DisplayName == "Popular Location 2");
    }

    [Fact]
    public async Task GetLocationSuggestionsAsync_Should_Return_Empty_For_Short_Query()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.GetLocationSuggestionsAsync("a"); // Too short

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_Should_Not_Throw_Exception()
    {
        // Arrange
        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act & Assert
        var disposing = () => service.Dispose();
        disposing.Should().NotThrow();
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_HttpRequestException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP error calling Nominatim API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Include_Zoom_Parameter_For_High_Detail()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new OpenStreetMapGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.ReverseGeocodeAsync(40.7831m, -73.9712m);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("zoom=18"); // High detail level
    }
}