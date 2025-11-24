using AutoMapper;
using BankingSystem.Application.Caching;
using BankingSystem.Application.Dtos;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using BankingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Infrastructure.Services;

public class AccountService(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IMapper mapper,
    ILogger<AccountService> logger,
    ICacheService cache) : IAccountService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<AccountService> _logger = logger;
    private readonly ICacheService _cache = cache;

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, string createdBy)
    {
        _logger.LogInformation("Creating new account for user {UserId}, type {AccountType}, initial deposit {InitialDeposit}",
            dto.UserId, dto.AccountType, dto.InitialDeposit);

        var account = _mapper.Map<Account>(dto);
        account.AccountNumber = GenerateAccountNumber();
        account.CreatedBy = createdBy;

        var existingAccount = await _unitOfWork.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);
        
        if (existingAccount != null)
        {
            _logger.LogWarning("Account creation failed: Duplicate account number {AccountNumber} generated", account.AccountNumber);
            throw new DuplicateAccountException(account.AccountNumber);
        }

        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        if (dto.InitialDeposit > 0)
        {
            var transaction = new Transaction
            {
                TransactionReference = GenerateTransactionReference(),
                AccountId = account.Id,
                Type = TransactionType.Deposit,
                Amount = dto.InitialDeposit,
                BalanceBefore = 0,
                BalanceAfter = dto.InitialDeposit,
                Description = "Initial deposit",
                Status = TransactionStatus.Completed,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            _logger.LogDebug("Initial deposit transaction created for account {AccountNumber}, amount {Amount}",
                account.AccountNumber, dto.InitialDeposit);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            _logger.LogDebug("Account {AccountNumber} created with zero initial deposit", account.AccountNumber);
        }

        _logger.LogInformation("Account created successfully: {AccountNumber}, user {UserId}, type {AccountType}, balance {Balance}",
            account.AccountNumber, dto.UserId, dto.AccountType, account.Balance);

        await _auditService.LogAsync("CreateAccount", "Account", account.Id.ToString(),
            null, $"Created account {account.AccountNumber}", createdBy);

        var accountDto = _mapper.Map<AccountDto>(account);

        var cacheKey = CacheKeys.Account(account.AccountNumber);
        _cache.Set(cacheKey, accountDto, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2));

        InvalidateUserAccountsCache(dto.UserId);

        return accountDto;
    }

    public async Task<AccountDto?> GetAccountByNumberAsync(string accountNumber)
    {
        var cacheKey = CacheKeys.Account(accountNumber);

        if (_cache.TryGetValue(cacheKey, out AccountDto? cachedAccount))
        {
            return cachedAccount;
        }

        var account = await _unitOfWork.Accounts.GetAccountWithUserAsync(accountNumber);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountNumber} not found", accountNumber);
            throw new AccountNotFoundException(accountNumber);
        }

        var accountDto = _mapper.Map<AccountDto>(account);

        _cache.Set(cacheKey, accountDto);

        return accountDto;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving account by ID {AccountId}", id);

        var account = await _unitOfWork.Accounts.GetAccountByIdWithUserAsync(id);
        
        if (account == null)
        {
            _logger.LogWarning("Account with ID {AccountId} not found", id);
            throw new AccountNotFoundException(id.ToString());
        }

        var accountDto = _mapper.Map<AccountDto>(account);

        if (!string.IsNullOrEmpty(account.AccountNumber))
        {
            var cacheKey = CacheKeys.Account(account.AccountNumber);
            _cache.Set(cacheKey, accountDto);
        }

        return accountDto;
    }

    public async Task<IEnumerable<AccountDto>> GetUserAccountsAsync(string userId)
    {
        var cacheKey = CacheKeys.UserAccounts(userId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<AccountDto>? cachedAccounts))
        {
            return cachedAccounts!;
        }

        var accounts = await _unitOfWork.Accounts.GetUserAccountsWithUserAsync(userId);
        var accountDtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);

        _cache.Set(cacheKey, accountDtos);

        return accountDtos;
    }

    public async Task<IEnumerable<AccountDto>> GetAllAccountsAsync()
    {
        _logger.LogDebug("Retrieving all accounts");

        var accounts = await _unitOfWork.Accounts.GetAllAccountsWithUserAsync();
        var result = _mapper.Map<IEnumerable<AccountDto>>(accounts);

        _logger.LogInformation("Retrieved {Count} total accounts", result.Count());
        return result;
    }

    public async Task<bool> UpdateAccountStatusAsync(int accountId, AccountStatus status, string updatedBy)
    {
        _logger.LogInformation("Updating account {AccountId} status to {Status} by user {UserId}",
            accountId, status, updatedBy);

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        
        if (account == null)
        {
            _logger.LogWarning("Account update failed: Account ID {AccountId} not found", accountId);
            throw new AccountNotFoundException(accountId.ToString());
        }

        var oldStatus = account.Status;
        account.Status = status;
        account.UpdatedBy = updatedBy;
        account.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Accounts.Update(account);
        
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating account {AccountId}", accountId);
            throw new InvalidOperationException("The account was modified by another user. Please refresh and try again.");
        }

        _logger.LogInformation("Account {AccountId} status updated successfully from {OldStatus} to {NewStatus}",
            accountId, oldStatus, status);

        await _auditService.LogAsync("UpdateAccountStatus", "Account", accountId.ToString(),
            $"Status: {oldStatus}", $"Status: {status}", updatedBy);

        InvalidateAccountCache(account.AccountNumber);
        InvalidateUserAccountsCache(account.UserId);

        return true;
    }

    private void InvalidateAccountCache(string accountNumber)
    {
        var cacheKey = CacheKeys.Account(accountNumber);
        _cache.Remove(cacheKey);
    }

    private void InvalidateUserAccountsCache(string userId)
    {
        var cacheKey = CacheKeys.UserAccounts(userId);
        _cache.Remove(cacheKey);
    }

    private static string GenerateAccountNumber()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }

    private static string GenerateTransactionReference()
    {
        return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
    }
}
