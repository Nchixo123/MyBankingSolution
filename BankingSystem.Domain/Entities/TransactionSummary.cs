namespace BankingSystem.Domain.Entities;

public class TransactionSummary
{
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalTransfersIn { get; set; }
    public decimal TotalTransfersOut { get; set; }
    public int TransactionCount { get; set; }
}
