using BankingSystem.Domain.Entities;
using BankingSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MyBankingSolution.Configuration;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        try
        {
            Log.Information("Initializing database and seeding data");

            var context = serviceProvider.GetRequiredService<BankDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();
            await SeedData.InitializeAsync(userManager, roleManager);
            
            Log.Information("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}