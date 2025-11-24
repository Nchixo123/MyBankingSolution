using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace MyBankingSolution.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services,
        IApiVersionDescriptionProvider? provider = null)
    {
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            if (provider != null)
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName, new OpenApiInfo
                    {
                        Title = "Banking System API",
                        Version = description.ApiVersion.ToString(),
                        Description = description.IsDeprecated 
                            ? "This API version has been deprecated." 
                            : "A comprehensive banking system API with JWT authentication",
                        Contact = new OpenApiContact
                        {
                            Name = "Banking System",
                            Email = "support@bankingsystem.com"
                        }
                    });
                }
            }
            else
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Banking System API",
                    Version = "v1",
                    Description = "A comprehensive banking system API with JWT authentication",
                    Contact = new OpenApiContact
                    {
                        Name = "Banking System",
                        Email = "support@bankingsystem.com"
                    }
                });
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Banking System API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "api/docs";
            options.DocumentTitle = "Banking System API Documentation";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
