using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using System.Linq.Expressions;

namespace BankingSystem.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<Transaction?> GetByReferenceAsync(string transactionReference);

    Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(
        int accountId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);

    Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(
        int accountId, 
        TransactionType type);

    Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(
        int accountId, 
        int count = 10);

    Task<IEnumerable<Transaction>> GetTransfersBetweenAccountsAsync(
        int fromAccountId, 
        int toAccountId);

    Task<IEnumerable<Transaction>> GetPendingTransactionsAsync();

    Task<TransactionSummary> GetTransactionSummaryAsync(
        int accountId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);

    Task<IEnumerable<Transaction>> GetLargeTransactionsAsync(
        decimal minimumAmount, 
        DateTime? fromDate = null);

    Task<IEnumerable<Transaction>> GetTransactionsByAccountNumberAsync(
        string accountNumber, 
        DateTime? from = null, 
        DateTime? to = null);

    Task<IEnumerable<Transaction>> GetAllTransactionsWithFiltersAsync(
        DateTime? from = null, 
        DateTime? to = null);

    IQueryable<Transaction> GetTransactionsQuery();
    IQueryable<Transaction> GetAccountTransactionsQuery(int accountId);
    IQueryable<Transaction> GetTransactionsQueryByDateRange(DateTime? fromDate, DateTime? toDate);
}