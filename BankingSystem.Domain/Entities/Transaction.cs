using BankingSystem.Domain.Entities.Enums;
using TransactionStatus = BankingSystem.Domain.Entities.Enums.TransactionStatus;

namespace BankingSystem.Domain.Entities;

public class Transaction : BaseEntity
{
    public string TransactionReference { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public int? RelatedAccountId { get; set; }
    public string? RelatedAccountNumber { get; set; }
    public Account? Account { get; set; }
}
