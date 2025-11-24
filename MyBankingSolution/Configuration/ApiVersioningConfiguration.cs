using Asp.Versioning;

namespace MyBankingSolution.Configuration;

public static class ApiVersioningConfiguration
{
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Default API version if not specified
            options.DefaultApiVersion = new ApiVersion(1, 0);
            
            // Assume default version when client doesn't specify
            options.AssumeDefaultVersionWhenUnspecified = true;
            
            // Report API versions in response headers
            options.ReportApiVersions = true;
            
            // API version reader - supports multiple methods
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/accounts
                new HeaderApiVersionReader("X-Api-Version"), // Header: X-Api-Version: 1.0
                new QueryStringApiVersionReader("api-version") // ?api-version=1.0
            );
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            // Format version as "'v'major[.minor][-status]"
            options.GroupNameFormat = "'v'VVV";
            
            // Substitute version in URL routes
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
