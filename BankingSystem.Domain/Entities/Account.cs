using BankingSystem.Domain.Entities.Enums;
using System.Transactions;

namespace BankingSystem.Domain.Entities;

public class Account : BaseEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public AccountStatus Status { get; set; }
    public string Currency { get; set; } = "USD";
    public byte[] RowVersion { get; set; } = null!;

    public ApplicationUser? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
