namespace BankingSystem.Application.Exceptions;

public class InsufficientFundsException(string accountNumber, decimal balance, decimal requestedAmount) : 
    BankingException($"Insufficient funds in account {accountNumber}. Available: {balance:C}, Requested: {requestedAmount:C}")
{
    public string AccountNumber { get; } = accountNumber;
    public decimal Balance { get; } = balance;
    public decimal RequestedAmount { get; } = requestedAmount;
}
