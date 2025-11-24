using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(
        string userId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _dbSet.Where(a => a.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType, 
        string entityId)
    {
        return await _dbSet
            .Where(a => a.Entity == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(
        string action, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _dbSet.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByIpAddressAsync(string ipAddress)
    {
        return await _dbSet
            .Where(a => a.IpAddress == ipAddress)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _dbSet
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<UserActivitySummary> GetUserActivitySummaryAsync(
        string userId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _dbSet.Where(a => a.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        var logs = await query.ToListAsync();

        var actionCounts = logs
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        return new UserActivitySummary
        {
            UserId = userId,
            UserName = logs.FirstOrDefault()?.UserName ?? string.Empty,
            TotalActions = logs.Count,
            FirstAction = logs.Min(a => (DateTime?)a.CreatedAt),
            LastAction = logs.Max(a => (DateTime?)a.CreatedAt),
            ActionCounts = actionCounts
        };
    }

    public async Task<IEnumerable<AuditLog>> GetSuspiciousActivitiesAsync(DateTime? fromDate = null)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        var allLogs = await query.ToListAsync();

        var suspiciousIps = allLogs
            .GroupBy(a => a.IpAddress)
            .Where(g => g.Count(a => a.Action.Contains("Failed") || a.Action.Contains("Delete")) > 3)
            .Select(g => g.Key)
            .ToHashSet();

        return allLogs
            .Where(a => suspiciousIps.Contains(a.IpAddress) ||
                       a.Action.Contains("Failed") ||
                       a.Action.Contains("Delete"))
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
    }

    public IQueryable<AuditLog> GetAuditLogsQuery()
    {
        return _dbSet.AsQueryable();
    }

    public IQueryable<AuditLog> GetAuditLogsByUserQuery(string userId)
    {
        return _dbSet
            .Where(a => a.UserId == userId)
            .AsQueryable();
    }

    public IQueryable<AuditLog> GetAuditLogsQueryByDateRange(DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return query;
    }
}
