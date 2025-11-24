using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;

namespace BankingSystem.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private IDbContextTransaction? _transaction;
    private Dictionary<Type, object>? _repositories;
    private readonly Lazy<IAccountRepository> _accountRepository;
    private readonly Lazy<ITransactionRepository> _transactionRepository;
    private readonly Lazy<IAuditLogRepository> _auditLogRepository;

    public UnitOfWork(DbContext context)
    {
        _context = context;
        
        _accountRepository = new Lazy<IAccountRepository>(() => new AccountRepository(_context));
        _transactionRepository = new Lazy<ITransactionRepository>(() => new TransactionRepository(_context));
        _auditLogRepository = new Lazy<IAuditLogRepository>(() => new AuditLogRepository(_context));
    }

    public IAccountRepository Accounts => _accountRepository.Value;
    public ITransactionRepository Transactions => _transactionRepository.Value;
    public IAuditLogRepository AuditLogs => _auditLogRepository.Value;

    public IRepository<T> Repository<T>() where T : class
    {
        _repositories ??= new Dictionary<Type, object>();

        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out object? value))
        {
            value = new Repository<T>(_context);
            _repositories[type] = value;
        }

        return (IRepository<T>)value;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
