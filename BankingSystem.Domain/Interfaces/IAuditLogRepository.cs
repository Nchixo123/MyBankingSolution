using BankingSystem.Domain.Entities;

namespace BankingSystem.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(
        string userId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);

    Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType, 
        string entityId);

    Task<IEnumerable<AuditLog>> GetByActionAsync(
        string action, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);

    Task<IEnumerable<AuditLog>> GetByIpAddressAsync(string ipAddress);

    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);

    Task<UserActivitySummary> GetUserActivitySummaryAsync(
        string userId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);

    Task<IEnumerable<AuditLog>> GetSuspiciousActivitiesAsync(DateTime? fromDate = null);

    IQueryable<AuditLog> GetAuditLogsQuery();

    IQueryable<AuditLog> GetAuditLogsByUserQuery(string userId);

    IQueryable<AuditLog> GetAuditLogsQueryByDateRange(DateTime? fromDate, DateTime? toDate);
}
