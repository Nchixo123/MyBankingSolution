using Asp.Versioning.ApiExplorer;
using MyBankingSolution.Configuration;
using MyBankingSolution.Middleware;
using Serilog;

SerilogConfiguration.ConfigureSerilog();

try
{
    Log.Information("Starting Banking System application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddHttpContextAccessor();

    builder.Host.AddSerilog();
    builder.Services.AddMvcConfiguration();
    builder.Services.AddDatabaseConfiguration(builder.Configuration);
    builder.Services.AddIdentityConfiguration();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();
    builder.Services.AddCachingConfiguration(builder.Configuration);
    builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
    
    builder.Services.AddApiVersioningConfiguration();
    
    builder.Services.AddSwaggerConfiguration();
    
    builder.Services.AddCorsConfiguration();
    
    builder.Services.AddHealthChecksConfiguration(builder.Configuration);
    
    builder.Services.AddRateLimitingConfiguration();

    var app = builder.Build();

    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    await app.Services.SeedDatabaseAsync();

    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerConfiguration(apiVersionDescriptionProvider);
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    
    app.UseCachingConfiguration();
    
    app.UseRateLimiter();
    
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecksEndpoints();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapControllers();

    Log.Information("Banking System application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
