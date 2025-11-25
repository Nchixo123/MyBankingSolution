using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Infrastructure.Repositories;

public class AccountRepository(BankDbContext context) : Repository<Account>(context), IAccountRepository
{
    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId && a.Status == AccountStatus.Active)
            .ToListAsync();
    }

    public async Task<Account?> GetAccountWithUserAsync(string accountNumber)
    {
        return await _dbSet
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<Account?> GetAccountByIdWithUserAsync(int id)
    {
        return await _dbSet
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Account>> GetUserAccountsWithUserAsync(string userId)
    {
        return await _dbSet
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetAllAccountsWithUserAsync()
    {
        return await _dbSet
            .Include(a => a.User)
            .ToListAsync();
    }

    public async Task<Account?> GetWithTransactionsAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Account>> GetAccountsByTypeAsync(AccountType accountType)
    {
        return await _dbSet
            .Where(a => a.AccountType == accountType)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetAccountsWithMinimumBalanceAsync(decimal minimumBalance)
    {
        return await _dbSet
            .Where(a => a.Balance >= minimumBalance && a.Status == AccountStatus.Active)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalBalanceByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId && a.Status == AccountStatus.Active)
            .SumAsync(a => a.Balance);
    }

    public async Task<IEnumerable<Account>> GetInactiveAccountsAsync()
    {
        return await _dbSet
            .Where(a => a.Status != AccountStatus.Active)
            .ToListAsync();
    }

    public IQueryable<Account> GetAccountsQuery()
    {
        return _dbSet.AsQueryable();
    }

    public IQueryable<Account> GetActiveAccountsQuery()
    {
        return _dbSet.Where(a => a.Status == AccountStatus.Active);
    }

    public IQueryable<Account> GetAccountsByUserIdQuery(string userId)
    {
        return _dbSet.Where(a => a.UserId == userId);
    }
}
