using AutoMapper;
using BankingSystem.Application.Caching;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Application.Validators;
using BankingSystem.Domain.Entities;
using BankingSystem.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using MyBankingSolution.Services;

namespace MyBankingSolution.Configuration;

public static class ApplicationServicesConfiguration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddScoped<UserManager<ApplicationUser>>();
        services.AddScoped<SignInManager<ApplicationUser>>();
        services.AddScoped<RoleManager<IdentityRole>>();

        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<DepositValidator>();

        AddCacheService(services, configuration, environment);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }

    private static void AddCacheService(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var useRedis = configuration.GetValue<bool>("Caching:UseRedis");

        if (environment.IsProduction() || useRedis)
        {
            services.AddSingleton<ICacheService, RedisCacheService>();
            Console.WriteLine("✅ Cache Service: Redis (Distributed) - Production/Multi-Server");
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
            Console.WriteLine("✅ Cache Service: In-Memory - Development/Single-Server");
        }
    }
}
