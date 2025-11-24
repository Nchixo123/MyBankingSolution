namespace BankingSystem.Application.Caching;

public static class CacheKeys
{
    public const string AccountPrefix = "account_";
    public const string UserAccountsPrefix = "user_accounts_";
    public const string TransactionPrefix = "transaction_";
    public const string AccountTransactionsPrefix = "account_transactions_";
    public const string DashboardStatsPrefix = "dashboard_stats_";

    public static string Account(string accountNumber) => $"{AccountPrefix}{accountNumber}";
    public static string UserAccounts(string userId) => $"{UserAccountsPrefix}{userId}";
    public static string AccountTransactions(string accountNumber) => $"{AccountTransactionsPrefix}{accountNumber}";
    public static string DashboardStats(string userId) => $"{DashboardStatsPrefix}{userId}";
}
