namespace BankingSystem.Application.Exceptions;

public class AccountInactiveException(string accountNumber) : BankingException($"Account {accountNumber} is not active")
{
    public string AccountNumber { get; } = accountNumber;
}
