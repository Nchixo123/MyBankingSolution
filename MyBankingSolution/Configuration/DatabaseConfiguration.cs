using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using DbContext = BankingSystem.Infrastructure.Data.DbContext;

namespace MyBankingSolution.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the specific DbContext
        services.AddDbContext<DbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("BankingSystem.Infrastructure")));

        // Register as base DbContext for repositories
        services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => 
            provider.GetRequiredService<DbContext>());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
