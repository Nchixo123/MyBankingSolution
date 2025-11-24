using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MyBankingSolution.Configuration;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 1. Global rate limit - applies to all requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (context.Request.Path.StartsWithSegments("/health"))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                var userIdentifier = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userIdentifier,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // 2. API rate limit - for API endpoints
            options.AddPolicy("api", context =>
            {
                var userIdentifier = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: userIdentifier,
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 50,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 50,
                        AutoReplenishment = true
                    });
            });

            // 3. Authentication rate limit - stricter for login/register
            options.AddPolicy("auth", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });

            // 4. Transaction rate limit - for financial operations
            options.AddPolicy("transaction", context =>
            {
                var userIdentifier = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: userIdentifier,
                    factory: _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 3,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            // 5. Strict rate limit - for sensitive operations
            options.AddPolicy("strict", context =>
            {
                var userIdentifier = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userIdentifier,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for strict policy
                    });
            });

            // Rejection behavior
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests",
                        message = "Rate limit exceeded. Please try again later.",
                        retryAfterSeconds = (int)retryAfter.TotalSeconds
                    }, cancellationToken);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests",
                        message = "Rate limit exceeded. Please try again later."
                    }, cancellationToken);
                }
            };
        });

        return services;
    }
}

public static class RateLimitPolicies
{
    public const string Api = "api";
    public const string Auth = "auth";
    public const string Transaction = "transaction";
    public const string Strict = "strict";
}
