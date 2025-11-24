using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BankingSystem.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Customer", "Staff" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@bankingsystem.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                Address = "123 Admin Street",
                DateOfBirth = new DateTime(1990, 1, 1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var customerEmail = "customer@example.com";
        var customerUser = await userManager.FindByEmailAsync(customerEmail);

        if (customerUser == null)
        {
            customerUser = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                FirstName = "John",
                LastName = "Doe",
                EmailConfirmed = true,
                PhoneNumber = "+1234567891",
                Address = "456 Customer Avenue",
                DateOfBirth = new DateTime(1995, 5, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(customerUser, "Customer@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }
        }
    }
}
