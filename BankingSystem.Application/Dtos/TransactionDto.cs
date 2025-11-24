using BankingSystem.Domain.Entities.Enums;

namespace BankingSystem.Application.Dtos;

public class TransactionDto
{
    public int Id { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string? RelatedAccountNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
