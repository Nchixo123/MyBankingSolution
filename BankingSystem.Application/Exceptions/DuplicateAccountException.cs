namespace BankingSystem.Application.Exceptions;

public class DuplicateAccountException(string accountNumber) : BankingException($"Account {accountNumber} already exists")
{
    public string AccountNumber { get; } = accountNumber;
}
