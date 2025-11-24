using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Interfaces;

namespace BankingSystem.Infrastructure.Services;

public class AuditService(IUnitOfWork unitOfWork) : IAuditService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task LogAsync(string action, string entity, string entityId,
        string? oldValues, string? newValues, string userId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            UserName = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress ?? "N/A", 
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();
    }
}
