using BankingSystem.Application.Dtos;

namespace BankingSystem.Application.Services.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto> DepositAsync(DepositDto dto, string createdBy);
    Task<TransactionDto> WithdrawAsync(WithdrawalDto dto, string createdBy);
    Task<(TransactionDto debit, TransactionDto credit)> TransferAsync(TransferDto dto, string createdBy);
    Task<IEnumerable<TransactionDto>> GetAccountTransactionsAsync(string accountNumber, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(DateTime? from = null, DateTime? to = null);
}
