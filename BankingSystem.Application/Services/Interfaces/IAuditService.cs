namespace BankingSystem.Application.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string entityId, string? oldValues, string? newValues, string userId, string? ipAddress = null);
}
