using Microsoft.Extensions.Caching.Memory;

namespace MyBankingSolution.Configuration;

public static class CachingConfiguration
{
    public static IServiceCollection AddCachingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024;
            options.CompactionPercentage = 0.25;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        var useRedis = configuration.GetValue<bool>("Caching:UseRedis");
        if (useRedis)
        {
            var redisConnection = configuration.GetConnectionString("Redis");

            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "BankingSystem_";
                });
            }
            else
            {
                Console.WriteLine("⚠️ Warning: Redis enabled but connection string not found. Falling back to in-memory cache.");
            }
        }

        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 1024 * 1024;
            options.UseCaseSensitivePaths = false;
        });

        return services;
    }

    public static IApplicationBuilder UseCachingConfiguration(this IApplicationBuilder app)
    {
        app.UseResponseCaching();
        return app;
    }
}

public static class CacheKeys
{
    public const string AccountPrefix = "account_";
    public const string UserAccountsPrefix = "user_accounts_";
    public const string TransactionPrefix = "transaction_";
    public const string AccountTransactionsPrefix = "account_transactions_";
    public const string DashboardStatsPrefix = "dashboard_stats_";

    public static string Account(string accountNumber) => $"{AccountPrefix}{accountNumber}";
    public static string UserAccounts(string userId) => $"{UserAccountsPrefix}{userId}";
    public static string AccountTransactions(string accountNumber) => $"{AccountTransactionsPrefix}{accountNumber}";
    public static string DashboardStats(string userId) => $"{DashboardStatsPrefix}{userId}";
}

public class CacheOptions
{
    public static MemoryCacheEntryOptions DefaultExpiration => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2),
        Size = 1
    };

    public static MemoryCacheEntryOptions ShortExpiration => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
        Size = 1
    };

    public static MemoryCacheEntryOptions LongExpiration => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(15),
        Size = 1
    };
}