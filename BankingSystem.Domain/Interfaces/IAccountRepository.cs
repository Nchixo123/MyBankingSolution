using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;

namespace BankingSystem.Domain.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<Account?> GetWithTransactionsAsync(int accountId);
    Task<IEnumerable<Account>> GetActiveAccountsByUserIdAsync(string userId);
    Task<IEnumerable<Account>> GetAccountsByTypeAsync(AccountType accountType);
    Task<IEnumerable<Account>> GetAccountsWithMinimumBalanceAsync(decimal minimumBalance);
    Task<decimal> GetTotalBalanceByUserIdAsync(string userId);
    Task<IEnumerable<Account>> GetInactiveAccountsAsync();
    IQueryable<Account> GetAccountsQuery();
    IQueryable<Account> GetActiveAccountsQuery();
    IQueryable<Account> GetAccountsByUserIdQuery(string userId);
    Task<Account?> GetAccountWithUserAsync(string accountNumber);
    Task<Account?> GetAccountByIdWithUserAsync(int id);
    Task<IEnumerable<Account>> GetUserAccountsWithUserAsync(string userId);
    Task<IEnumerable<Account>> GetAllAccountsWithUserAsync();
}
