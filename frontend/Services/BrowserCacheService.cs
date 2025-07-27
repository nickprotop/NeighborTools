using Microsoft.JSInterop;
using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public interface IBrowserCacheService
{
    Task<PagedResult<T>?> GetCachedPageAsync<T>(string cacheKey);
    Task SetCachedPageAsync<T>(string cacheKey, PagedResult<T> data, TimeSpan? expiry = null);
    Task<T?> GetCachedDataAsync<T>(string cacheKey);
    Task SetCachedDataAsync<T>(string cacheKey, T data, TimeSpan? expiry = null);
    Task InvalidateByPrefixAsync(string prefix);
    Task ClearExpiredAsync();
    Task<long> GetCacheSizeAsync();
    string GeneratePageCacheKey(string entityType, int page, int pageSize, Dictionary<string, string?> parameters);
}

public class BrowserCacheService : IBrowserCacheService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Cache configuration
    private const int MAX_CACHE_ENTRIES = 50;
    private const long MAX_STORAGE_SIZE = 5 * 1024 * 1024; // 5MB
    private static readonly TimeSpan DEFAULT_EXPIRY = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DEVICE_INFO_EXPIRY = TimeSpan.FromMinutes(30);

    public BrowserCacheService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/browserCache.js");
        }
        return _module;
    }

    public async Task<PagedResult<T>?> GetCachedPageAsync<T>(string cacheKey)
    {
        try
        {
            var module = await GetModuleAsync();
            var cachedData = await module.InvokeAsync<string?>("browserCache.getItem", cacheKey);
            
            if (string.IsNullOrEmpty(cachedData))
                return null;

            var cacheEntry = JsonSerializer.Deserialize<CacheEntry<PagedResult<T>>>(cachedData, _jsonOptions);
            
            if (cacheEntry == null || IsExpired(cacheEntry.ExpiresAt))
            {
                await InvalidateCacheKeyAsync(cacheKey);
                return null;
            }

            return cacheEntry.Data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SetCachedPageAsync<T>(string cacheKey, PagedResult<T> data, TimeSpan? expiry = null)
    {
        try
        {
            expiry ??= DEFAULT_EXPIRY;
            var expiresAt = DateTime.UtcNow.Add(expiry.Value);
            
            var cacheEntry = new CacheEntry<PagedResult<T>>
            {
                Data = data,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            var serializedData = JsonSerializer.Serialize(cacheEntry, _jsonOptions);
            
            // Check storage limits before adding
            await EnsureStorageCapacityAsync();
            
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("browserCache.setItem", cacheKey, serializedData);
        }
        catch (Exception)
        {
            // Silently fail caching to not impact user experience
        }
    }

    public async Task<T?> GetCachedDataAsync<T>(string cacheKey)
    {
        try
        {
            var module = await GetModuleAsync();
            var cachedData = await module.InvokeAsync<string?>("browserCache.getItem", cacheKey);
            
            if (string.IsNullOrEmpty(cachedData))
                return default(T);

            var cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>(cachedData, _jsonOptions);
            
            if (cacheEntry == null || IsExpired(cacheEntry.ExpiresAt))
            {
                await InvalidateCacheKeyAsync(cacheKey);
                return default(T);
            }

            return cacheEntry.Data;
        }
        catch (Exception)
        {
            return default(T);
        }
    }

    public async Task SetCachedDataAsync<T>(string cacheKey, T data, TimeSpan? expiry = null)
    {
        try
        {
            expiry ??= DEFAULT_EXPIRY;
            var expiresAt = DateTime.UtcNow.Add(expiry.Value);
            
            var cacheEntry = new CacheEntry<T>
            {
                Data = data,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            var serializedData = JsonSerializer.Serialize(cacheEntry, _jsonOptions);
            
            await EnsureStorageCapacityAsync();
            
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("browserCache.setItem", cacheKey, serializedData);
        }
        catch (Exception)
        {
            // Silently fail caching to not impact user experience
        }
    }

    public async Task InvalidateByPrefixAsync(string prefix)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("browserCache.removeByPrefix", prefix);
        }
        catch (Exception)
        {
            // Silently fail invalidation
        }
    }

    public async Task ClearExpiredAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("browserCache.clearExpired");
        }
        catch (Exception)
        {
            // Silently fail cleanup
        }
    }

    public async Task<long> GetCacheSizeAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<long>("browserCache.getStorageSize");
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public string GeneratePageCacheKey(string entityType, int page, int pageSize, Dictionary<string, string?> parameters)
    {
        var keyParts = new List<string> { entityType, $"page_{page}", $"size_{pageSize}" };
        
        foreach (var param in parameters.OrderBy(p => p.Key))
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                keyParts.Add($"{param.Key}_{param.Value}");
            }
        }
        
        return string.Join("_", keyParts);
    }

    private async Task InvalidateCacheKeyAsync(string cacheKey)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("browserCache.removeItem", cacheKey);
        }
        catch (Exception)
        {
            // Silently fail invalidation
        }
    }

    private async Task EnsureStorageCapacityAsync()
    {
        try
        {
            var currentSize = await GetCacheSizeAsync();
            if (currentSize > MAX_STORAGE_SIZE)
            {
                var module = await GetModuleAsync();
                await module.InvokeVoidAsync("browserCache.clearLRU", MAX_CACHE_ENTRIES / 2);
            }
        }
        catch (Exception)
        {
            // Silently fail capacity management
        }
    }

    private static bool IsExpired(DateTime expiresAt)
    {
        return DateTime.UtcNow > expiresAt;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}

public class CacheEntry<T>
{
    public T Data { get; set; } = default(T)!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}