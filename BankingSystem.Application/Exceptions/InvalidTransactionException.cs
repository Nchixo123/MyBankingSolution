namespace BankingSystem.Application.Exceptions;

public class InvalidTransactionException(string message) : BankingException(message)
{

}
