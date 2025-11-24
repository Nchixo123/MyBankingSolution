using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using BankingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(BankingSystem.Infrastructure.Data.DbContext context) : base(context)
    {
    }

    public async Task<Transaction?> GetByReferenceAsync(string transactionReference)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TransactionReference == transactionReference);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByAccountNumberAsync(
        string accountNumber, 
        DateTime? from = null, 
        DateTime? to = null)
    {
        var account = await _context.Set<Account>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null)
            return Enumerable.Empty<Transaction>();

        var query = _dbSet
            .AsNoTracking()
            .Where(t => t.AccountId == account.Id);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsWithFiltersAsync(
        DateTime? from = null, 
        DateTime? to = null)
    {
        var query = _dbSet.AsNoTracking();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(
        int accountId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(t => t.AccountId == accountId);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(int accountId, TransactionType type)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId && t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(int accountId, int count = 10)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransfersBetweenAccountsAsync(int fromAccountId, int toAccountId)
    {
        return await _dbSet
            .Where(t => (t.AccountId == fromAccountId && t.RelatedAccountId == toAccountId) ||
                       (t.AccountId == toAccountId && t.RelatedAccountId == fromAccountId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync()
    {
        return await _dbSet
            .Where(t => t.Status == TransactionStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TransactionSummary> GetTransactionSummaryAsync(
        int accountId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(t => t.AccountId == accountId);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var transactions = await query.ToListAsync();

        return new TransactionSummary
        {
            TotalDeposits = transactions.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount),
            TotalWithdrawals = transactions.Where(t => t.Type == TransactionType.Withdrawal).Sum(t => t.Amount),
            TotalTransfersIn = transactions.Where(t => t.Type == TransactionType.TransferIn).Sum(t => t.Amount),
            TotalTransfersOut = transactions.Where(t => t.Type == TransactionType.TransferOut).Sum(t => t.Amount),
            TransactionCount = transactions.Count
        };
    }

    public async Task<IEnumerable<Transaction>> GetLargeTransactionsAsync(decimal minimumAmount, DateTime? fromDate = null)
    {
        var query = _dbSet
            .Where(t => t.Amount >= minimumAmount);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        return await query
            .OrderByDescending(t => t.Amount)
            .ToListAsync();
    }

    public IQueryable<Transaction> GetTransactionsQuery()
    {
        return _dbSet.AsQueryable();
    }

    public IQueryable<Transaction> GetAccountTransactionsQuery(int accountId)
    {
        return _dbSet.Where(t => t.AccountId == accountId);
    }

    public IQueryable<Transaction> GetTransactionsQueryByDateRange(DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        return query;
    }
}
