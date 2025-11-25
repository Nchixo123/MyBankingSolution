namespace MyBankingSolution.Configuration;

public static class MvcConfiguration
{
    public static IServiceCollection AddMvcConfiguration(this IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                
                // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        return services;
    }
}
