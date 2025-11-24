namespace BankingSystem.Application.Caching;

public interface ICacheService
{

    bool TryGetValue<T>(string key, out T? value);
    void Set<T>(string key, T value);
    void Set<T>(string key, T value, TimeSpan absoluteExpiration);
    void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan? slidingExpiration);
    void Remove(string key);
    void RemoveByPattern(string pattern);
}
