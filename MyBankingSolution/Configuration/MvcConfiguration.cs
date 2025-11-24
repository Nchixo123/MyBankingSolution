namespace MyBankingSolution.Configuration;

public static class MvcConfiguration
{
    public static IServiceCollection AddMvcConfiguration(this IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Prevent circular reference issues when serializing entities with navigation properties
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                
                // Optional: Use camelCase for JSON properties
                // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        return services;
    }
}
