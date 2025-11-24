using BankingSystem.Domain.Entities;

namespace BankingSystem.Application.Services.Interfaces;


public interface IJwtService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
