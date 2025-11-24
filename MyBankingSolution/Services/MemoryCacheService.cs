using BankingSystem.Application.Caching;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace MyBankingSolution.Services;


public class MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<MemoryCacheService> _logger = logger;
    
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public bool TryGetValue<T>(string key, out T? value)
    {
        var found = _cache.TryGetValue(key, out value);

        if (found)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", key);
        }
        else
        {
            _logger.LogDebug("Cache MISS: {CacheKey}", key);
        }

        return found;
    }

    public void Set<T>(string key, T value)
    {
        Set(key, value, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2));
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration)
    {
        Set(key, value, absoluteExpiration, null);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan? slidingExpiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            SlidingExpiration = slidingExpiration,
            Size = 1,
            Priority = CacheItemPriority.Normal
        };

        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            _keys.TryRemove(k.ToString()!, out _);
            _logger.LogDebug("Cache entry evicted: {CacheKey}, Reason: {Reason}", k, reason);
        });

        _cache.Set(key, value, options);
        _keys.TryAdd(key, 0);

        _logger.LogDebug("Cached: {CacheKey} (Absolute: {Absolute}, Sliding: {Sliding})",
            key, absoluteExpiration, slidingExpiration?.ToString() ?? "None");
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("Invalidated cache: {CacheKey}", key);
    }

    public void RemoveByPattern(string pattern)
    {
        var patternPrefix = pattern.Replace("*", "", StringComparison.OrdinalIgnoreCase);
        
        var keysToRemove = _keys.Keys
            .Where(k => k.StartsWith(patternPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}",
            keysToRemove.Count, pattern);
    }
}

public static class CacheExpiration
{
    public static (TimeSpan Absolute, TimeSpan? Sliding) Default =>
        (TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2));

    public static (TimeSpan Absolute, TimeSpan? Sliding) Short =>
        (TimeSpan.FromMinutes(1), null);

    public static (TimeSpan Absolute, TimeSpan? Sliding) Long =>
        (TimeSpan.FromHours(1), TimeSpan.FromMinutes(15));
}
