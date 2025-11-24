namespace BankingSystem.Domain.Entities.Enums;

public enum AccountType
{
    Savings,
    Checking,
    BusinessChecking,
    MoneyMarket
}

public enum AccountStatus
{
    Active,
    Inactive,
    Frozen,
    Closed
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    TransferIn,
    TransferOut
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Reversed
}
