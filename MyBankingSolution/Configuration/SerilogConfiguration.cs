using Serilog;
using Serilog.Events;

namespace MyBankingSolution.Configuration;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "BankingSystem")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{RequestId}] {Message:lj} <{SourceContext}>{NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/banking-system-.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10485760,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{RequestId}] [{UserName}@{IpAddress}] {HttpMethod} {RequestPath} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/requests-.log",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{RequestId}] {UserName}@{IpAddress} {HttpMethod} {RequestPath} {Message:lj}{NewLine}")
            .CreateLogger();
    }

    public static IHostBuilder AddSerilog(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "BankingSystem"));

        return host;
    }

    public static IApplicationBuilder UseSerilogRequestLoggingConfiguration(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
                if (elapsed > 1000) return LogEventLevel.Warning;
                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "Anonymous");
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
                diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
                diagnosticContext.Set("QueryString", httpContext.Request.QueryString.ToString());
                diagnosticContext.Set("ContentType", httpContext.Request.ContentType);
                diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType);
                
                if (httpContext.Items.TryGetValue("RequestId", out var requestId))
                {
                    diagnosticContext.Set("RequestId", requestId);
                }
            };
        });

        return app;
    }
}
