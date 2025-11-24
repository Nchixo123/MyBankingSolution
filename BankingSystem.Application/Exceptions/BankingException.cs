namespace BankingSystem.Application.Exceptions;

public class BankingException : Exception
{
    public BankingException(string message) : base(message)
    {
    }

    public BankingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
