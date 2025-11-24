namespace BankingSystem.Application.Exceptions;

public class AccountNotFoundException(string accountNumber) : BankingException($"Account {accountNumber} not found")
{
    public string AccountNumber { get; } = accountNumber;
}
