using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.Data;
using BankingSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;


namespace MyBankingSolution.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the specific DbContext
        services.AddDbContext<BankDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("BankingSystem.Infrastructure")));

        // Register as base DbContext for repositories
        services.AddScoped<DbContext>(provider => 
            provider.GetRequiredService<BankDbContext>());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
