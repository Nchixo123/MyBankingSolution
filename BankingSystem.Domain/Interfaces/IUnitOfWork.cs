using BankingSystem.Domain.Entities;

namespace BankingSystem.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    IAuditLogRepository AuditLogs { get; }
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
