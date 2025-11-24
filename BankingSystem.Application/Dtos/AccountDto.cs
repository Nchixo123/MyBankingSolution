using BankingSystem.Domain.Entities.Enums;

namespace BankingSystem.Application.Dtos
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public AccountStatus Status { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime CreatedAt { get; set; }
    }
}
