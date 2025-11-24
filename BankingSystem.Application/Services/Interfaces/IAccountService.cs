using BankingSystem.Application.Dtos;
using BankingSystem.Domain.Entities.Enums;

namespace BankingSystem.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, string createdBy);
    Task<AccountDto?> GetAccountByNumberAsync(string accountNumber);
    Task<AccountDto?> GetAccountByIdAsync(int id);
    Task<IEnumerable<AccountDto>> GetUserAccountsAsync(string userId);
    Task<IEnumerable<AccountDto>> GetAllAccountsAsync();
    Task<bool> UpdateAccountStatusAsync(int accountId, AccountStatus status, string updatedBy);
}
