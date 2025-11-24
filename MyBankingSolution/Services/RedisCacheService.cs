using BankingSystem.Application.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MyBankingSolution.Services;


public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<RedisCacheService> _logger = logger;

    public bool TryGetValue<T>(string key, out T? value)
    {
        try
        {
            var json = _cache.GetString(key);
            
            if (!string.IsNullOrEmpty(json))
            {
                value = JsonSerializer.Deserialize<T>(json);
                _logger.LogDebug("Cache HIT (Redis): {CacheKey}", key);
                return true;
            }

            value = default;
            _logger.LogDebug("Cache MISS (Redis): {CacheKey}", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis cache read error for key: {CacheKey}", key);
            value = default;
            return false;
        }
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
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };

            _cache.SetString(key, json, options);
            _logger.LogDebug("Cached to Redis: {CacheKey} (Absolute: {Absolute}, Sliding: {Sliding})",
                key, absoluteExpiration, slidingExpiration?.ToString() ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis cache write error for key: {CacheKey}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Invalidated Redis cache: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis cache remove error for key: {CacheKey}", key);
        }
    }

    public void RemoveByPattern(string pattern)
    {
        _logger.LogWarning("RemoveByPattern not fully implemented for Redis. Pattern: {Pattern}", pattern);
    }
}
