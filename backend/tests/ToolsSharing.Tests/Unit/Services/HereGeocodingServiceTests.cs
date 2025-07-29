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
/// Unit tests for HereGeocodingService
/// </summary>
public class HereGeocodingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<HereGeocodingService>> _loggerMock;
    private readonly HereConfiguration _config;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public HereGeocodingServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Create logger mock
        _loggerMock = new Mock<ILogger<HereGeocodingService>>();

        // Create test configuration with API key
        _config = new HereConfiguration
        {
            BaseUrl = "https://geocode.search.hereapi.com/v1",
            ApiKey = "test-api-key-12345",
            RequestsPerSecond = 10,
            CacheDurationHours = 24,
            TimeoutSeconds = 30,
            DefaultLanguage = "en",
            MaxResults = 20,
            IncludeAddressDetails = true,
            IncludeMapView = true
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
    public void ProviderName_Should_Return_HERE()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var providerName = service.ProviderName;

        // Assert
        providerName.Should().Be("HERE");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Return_Empty_List_For_Empty_Query()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.SearchLocationsAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Return_Empty_List_When_API_Key_Not_Configured()
    {
        // Arrange
        var configWithoutApiKey = new HereConfiguration
        {
            ApiKey = "", // Empty API key
            BaseUrl = _config.BaseUrl
        };
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, configWithoutApiKey);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HERE API key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Return_Cached_Results_When_Available()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResults = new List<LocationOption>
        {
            new() { DisplayName = "Cached HERE Location", Lat = 40.7831m, Lng = -73.9712m }
        };
        
        var cacheKey = "here_search:New York:5:";
        _cache.Set(cacheKey, cachedResults);

        // Act
        var result = await service.SearchLocationsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DisplayName.Should().Be("Cached HERE Location");
        
        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_Successful_API_Response()
    {
        // Arrange
        var hereResponse = new HereGeocodingResponse
        {
            Items = new List<HereGeocodingResult>
            {
                new()
                {
                    Title = "New York, NY, USA",
                    Position = new HerePosition { Lat = 40.7831m, Lng = -73.9712m },
                    Address = new HereAddress
                    {
                        City = "New York",
                        State = "New York",
                        CountryName = "United States",
                        CountryCode = "USA"
                    },
                    Scoring = new HereScoring
                    {
                        QueryScore = 0.95m
                    }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(hereResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

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
        result.First().Confidence.Should().Be(0.95m);
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Include_API_Key_In_Request()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain($"apiKey={_config.ApiKey}");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Include_Country_Code_When_Provided()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 5, "US");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("in=countryCode:US");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Respect_Limit_Parameter()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 10);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("limit=10");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Cap_Limit_At_MaxResults()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York", 100); // Request more than MaxResults (20)

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain($"limit={_config.MaxResults}"); // Should be capped at MaxResults
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_API_Error_Response()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            ReasonPhrase = "Unauthorized"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HERE API error")),
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

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

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
    public async Task ReverseGeocodeAsync_Should_Return_Null_When_API_Key_Not_Configured()
    {
        // Arrange
        var configWithoutApiKey = new HereConfiguration
        {
            ApiKey = "", // Empty API key
            BaseUrl = _config.BaseUrl
        };
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, configWithoutApiKey);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7831m, -73.9712m);

        // Assert
        result.Should().BeNull();
        
        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HERE API key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Return_Cached_Result_When_Available()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResult = new LocationOption
        {
            DisplayName = "Cached HERE Location",
            Lat = 40.7831m,
            Lng = -73.9712m
        };
        
        var cacheKey = "here_reverse:40.783100:-73.971200";
        _cache.Set(cacheKey, cachedResult);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7831m, -73.9712m);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Cached HERE Location");
        
        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReverseGeocodeAsync_Should_Handle_Successful_API_Response()
    {
        // Arrange
        var hereResponse = new HereGeocodingResponse
        {
            Items = new List<HereGeocodingResult>
            {
                new()
                {
                    Title = "Empire State Building, New York, NY, USA",
                    Position = new HerePosition { Lat = 40.7484m, Lng = -73.9857m },
                    Address = new HereAddress
                    {
                        Street = "5th Avenue",
                        City = "New York",
                        State = "New York",
                        CountryName = "United States"
                    }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(hereResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

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
    public async Task GetLocationSuggestionsAsync_Should_Return_Empty_For_Short_Query()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.GetLocationSuggestionsAsync("a"); // Too short

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLocationSuggestionsAsync_Should_Use_Autosuggest_Endpoint()
    {
        // Arrange
        var autosuggestResponse = new HereAutosuggestResponse
        {
            Items = new List<HereAutosuggestItem>
            {
                new()
                {
                    Title = "New York, NY, USA",
                    ResultType = "place",
                    Position = new HerePosition { Lat = 40.7831m, Lng = -73.9712m },
                    Address = new HereAddress
                    {
                        City = "New York",
                        State = "New York",
                        CountryName = "United States"
                    },
                    Scoring = new HereScoring { QueryScore = 0.9m }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(autosuggestResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        var result = await service.GetLocationSuggestionsAsync("New York");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DisplayName.Should().Be("New York, NY, USA");
        
        // Verify autosuggest endpoint was used
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("/autosuggest");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Handle_HttpRequestException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP error calling HERE API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPopularLocationsAsync_Should_Return_Cached_Results_When_Available()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);
        var cachedResults = new List<LocationOption>
        {
            new() { DisplayName = "Popular HERE Location 1" },
            new() { DisplayName = "Popular HERE Location 2" }
        };
        
        var cacheKey = "here_popular:10";
        _cache.Set(cacheKey, cachedResults);

        // Act
        var result = await service.GetPopularLocationsAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.DisplayName == "Popular HERE Location 1");
        result.Should().Contain(x => x.DisplayName == "Popular HERE Location 2");
    }

    [Fact]
    public void Dispose_Should_Not_Throw_Exception()
    {
        // Arrange
        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act & Assert
        var disposing = () => service.Dispose();
        disposing.Should().NotThrow();
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Include_Address_Details_When_Configured()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("show=details");
    }

    [Fact]
    public async Task SearchLocationsAsync_Should_Include_Map_View_When_Configured()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new HereGeocodingResponse { Items = new List<HereGeocodingResult>() }))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        var service = new HereGeocodingService(_httpClient, _context, _cache, _loggerMock.Object, _config);

        // Act
        await service.SearchLocationsAsync("New York");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("show=areas");
    }
}