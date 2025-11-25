using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using BankingSystem.Infrastructure.Data;

namespace MyBankingSolution.Configuration;

public static class HealthChecksConfiguration
{
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // 1. Database Health Check
        healthChecksBuilder.AddDbContextCheck<BankDbContext>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "ready" });

        // 2. SQL Server Health Check (more detailed)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddSqlServer(
                connectionString,
                healthQuery: "SELECT 1",
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" },
                timeout: TimeSpan.FromSeconds(3));
        }

        // 3. Redis Health Check (if enabled)
        var useRedis = configuration.GetValue<bool>("Caching:UseRedis");
        if (useRedis)
        {
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                healthChecksBuilder.AddRedis(
                    redisConnection,
                    name: "redis",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "cache", "redis", "ready" },
                    timeout: TimeSpan.FromSeconds(2));
            }
        }

        // 4. Memory Health Check
        healthChecksBuilder.AddCheck(
            name: "memory",
            check: () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var threshold = 1024L * 1024L * 1024L; // 1 GB

                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory usage: {allocated / 1024 / 1024} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {allocated / 1024 / 1024} MB");
            },
            tags: new[] { "memory", "live" });

        // 5. Disk Space Health Check
        healthChecksBuilder.AddCheck(
            name: "disk",
            check: () =>
            {
                var drive = new DriveInfo(Directory.GetCurrentDirectory());
                var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var threshold = 5; // 5 GB minimum

                return freeSpaceGB > threshold
                    ? HealthCheckResult.Healthy($"Free disk space: {freeSpaceGB} GB")
                    : HealthCheckResult.Degraded($"Low disk space: {freeSpaceGB} GB");
            },
            tags: new[] { "disk", "live" });

        // 6. Health Checks UI (optional dashboard)
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Banking System API", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 1. Readiness probe - Is the app ready to accept requests?
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            AllowCachingResponses = false
        });

        // 2. Liveness probe - Is the app running?
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            AllowCachingResponses = false
        });

        // 3. Detailed health check - All checks with full details
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = false
        });

        // 4. Simple health check - Just returns OK/Unhealthy
        endpoints.MapHealthChecks("/health/simple", new HealthCheckOptions
        {
            Predicate = _ => true,
            AllowCachingResponses = false
        });

        // 5. Health Checks UI (dashboard)
        endpoints.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-api";
        });

        return endpoints;
    }
}
