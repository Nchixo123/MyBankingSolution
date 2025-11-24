using BankingSystem.Domain.Entities.Enums;

namespace BankingSystem.Application.Dtos;

public class CreateAccountDto
{
    public string UserId { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal InitialDeposit { get; set; }
}
