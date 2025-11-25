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

public class TransactionService(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IMapper mapper,
    ILogger<TransactionService> logger,
    ICacheService cache) : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<TransactionService> _logger = logger;
    private readonly ICacheService _cache = cache;

    public async Task<TransactionDto> DepositAsync(DepositDto dto, string createdBy)
    {
        _logger.LogInformation("Initiating deposit transaction for account {AccountNumber}, amount {Amount}, user {UserId}",
            dto.AccountNumber, dto.Amount, createdBy);

        var account = await _unitOfWork.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == dto.AccountNumber);

        if (account == null)
        {
            _logger.LogWarning("Deposit failed: Account {AccountNumber} not found", dto.AccountNumber);
            throw new AccountNotFoundException(dto.AccountNumber);
        }

        if (account.UserId != createdBy)
        {
            _logger.LogWarning("Deposit failed: User {UserId} does not own account {AccountNumber}", createdBy, dto.AccountNumber);
            throw new UnauthorizedAccessException("You do not have permission to deposit to this account.");
        }

        if (account.Status != AccountStatus.Active)
        {
            _logger.LogWarning("Deposit failed: Account {AccountNumber} is not active, status: {Status}",
                dto.AccountNumber, account.Status);
            throw new AccountInactiveException(dto.AccountNumber);
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var balanceBefore = account.Balance;
            account.Balance += dto.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = createdBy;

            var transaction = _mapper.Map<Transaction>(dto);
            transaction.TransactionReference = GenerateTransactionReference();
            transaction.AccountId = account.Id;
            transaction.BalanceBefore = balanceBefore;
            transaction.BalanceAfter = account.Balance;
            transaction.CreatedBy = createdBy;

            _unitOfWork.Accounts.Update(account);
            await _unitOfWork.Transactions.AddAsync(transaction);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Deposit completed successfully: {TransactionReference}, Account {AccountNumber}, " +
                "Amount {Amount}, Balance changed from {BalanceBefore} to {BalanceAfter}",
                transaction.TransactionReference, dto.AccountNumber, dto.Amount, balanceBefore, account.Balance);

            await _auditService.LogAsync("Deposit", "Transaction", transaction.Id.ToString(),
                null, $"Deposited {dto.Amount:C} to {dto.AccountNumber}", createdBy);

            var result = _mapper.Map<TransactionDto>(transaction);
            result.AccountNumber = dto.AccountNumber;

            InvalidateAccountCache(dto.AccountNumber);
            InvalidateUserAccountsCache(account.UserId);
            InvalidateAccountTransactionsCache(dto.AccountNumber);
            InvalidateDashboardCache(account.UserId);

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Concurrency conflict during deposit for account {AccountNumber}", dto.AccountNumber);
            throw new InvalidOperationException("The account was modified by another transaction. Please try again.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Deposit transaction failed for account {AccountNumber}, amount {Amount}",
                dto.AccountNumber, dto.Amount);
            throw;
        }
    }

    public async Task<TransactionDto> WithdrawAsync(WithdrawalDto dto, string createdBy)
    {
        _logger.LogInformation("Initiating withdrawal transaction for account {AccountNumber}, amount {Amount}, user {UserId}",
            dto.AccountNumber, dto.Amount, createdBy);

        var account = await _unitOfWork.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == dto.AccountNumber);

        if (account == null)
        {
            _logger.LogWarning("Withdrawal failed: Account {AccountNumber} not found", dto.AccountNumber);
            throw new AccountNotFoundException(dto.AccountNumber);
        }

        if (account.UserId != createdBy)
        {
            _logger.LogWarning("Withdrawal failed: User {UserId} does not own account {AccountNumber}", createdBy, dto.AccountNumber);
            throw new UnauthorizedAccessException("You do not have permission to withdraw from this account.");
        }

        if (account.Status != AccountStatus.Active)
        {
            _logger.LogWarning("Withdrawal failed: Account {AccountNumber} is not active, status: {Status}",
                dto.AccountNumber, account.Status);
            throw new AccountInactiveException(dto.AccountNumber);
        }

        if (account.Balance < dto.Amount)
        {
            _logger.LogWarning("Withdrawal failed: Insufficient funds in account {AccountNumber}, " +
                "Balance: {Balance}, Requested: {RequestedAmount}",
                dto.AccountNumber, account.Balance, dto.Amount);
            throw new InsufficientFundsException(dto.AccountNumber, account.Balance, dto.Amount);
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var balanceBefore = account.Balance;
            account.Balance -= dto.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = createdBy;

            var transaction = _mapper.Map<Transaction>(dto);
            transaction.TransactionReference = GenerateTransactionReference();
            transaction.AccountId = account.Id;
            transaction.BalanceBefore = balanceBefore;
            transaction.BalanceAfter = account.Balance;
            transaction.CreatedBy = createdBy;

            _unitOfWork.Accounts.Update(account);
            await _unitOfWork.Transactions.AddAsync(transaction);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Withdrawal completed successfully: {TransactionReference}, Account {AccountNumber}, " +
                "Amount {Amount}, Balance changed from {BalanceBefore} to {BalanceAfter}",
                transaction.TransactionReference, dto.AccountNumber, dto.Amount, balanceBefore, account.Balance);

            await _auditService.LogAsync("Withdrawal", "Transaction", transaction.Id.ToString(),
                null, $"Withdrew {dto.Amount:C} from {dto.AccountNumber}", createdBy);

            var result = _mapper.Map<TransactionDto>(transaction);
            result.AccountNumber = dto.AccountNumber;

            InvalidateAccountCache(dto.AccountNumber);
            InvalidateUserAccountsCache(account.UserId);
            InvalidateAccountTransactionsCache(dto.AccountNumber);
            InvalidateDashboardCache(account.UserId);

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Concurrency conflict during withdrawal for account {AccountNumber}", dto.AccountNumber);
            throw new InvalidOperationException("The account was modified by another transaction. Please try again.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Withdrawal transaction failed for account {AccountNumber}, amount {Amount}",
                dto.AccountNumber, dto.Amount);
            throw;
        }
    }

    public async Task<(TransactionDto debit, TransactionDto credit)> TransferAsync(TransferDto dto, string createdBy)
    {
        _logger.LogInformation("Initiating transfer transaction from {FromAccount} to {ToAccount}, amount {Amount}, user {UserId}",
            dto.FromAccountNumber, dto.ToAccountNumber, dto.Amount, createdBy);

        var fromAccount = await _unitOfWork.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == dto.FromAccountNumber);

        var toAccount = await _unitOfWork.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == dto.ToAccountNumber);

        if (fromAccount == null)
        {
            _logger.LogWarning("Transfer failed: Source account {AccountNumber} not found", dto.FromAccountNumber);
            throw new AccountNotFoundException(dto.FromAccountNumber);
        }

        if (toAccount == null)
        {
            _logger.LogWarning("Transfer failed: Destination account {AccountNumber} not found", dto.ToAccountNumber);
            throw new AccountNotFoundException(dto.ToAccountNumber);
        }

        if (fromAccount.UserId != createdBy)
        {
            _logger.LogWarning("Transfer failed: User {UserId} does not own source account {AccountNumber}", createdBy, dto.FromAccountNumber);
            throw new UnauthorizedAccessException("You do not have permission to transfer from this account.");
        }

        if (fromAccount.Status != AccountStatus.Active)
        {
            _logger.LogWarning("Transfer failed: Source account {AccountNumber} is not active, status: {Status}",
                dto.FromAccountNumber, fromAccount.Status);
            throw new AccountInactiveException(dto.FromAccountNumber);
        }

        if (toAccount.Status != AccountStatus.Active)
        {
            _logger.LogWarning("Transfer failed: Destination account {AccountNumber} is not active, status: {Status}",
                dto.ToAccountNumber, toAccount.Status);
            throw new AccountInactiveException(dto.ToAccountNumber);
        }

        if (fromAccount.Balance < dto.Amount)
        {
            _logger.LogWarning("Transfer failed: Insufficient funds in source account {AccountNumber}, " +
                "Balance: {Balance}, Requested: {RequestedAmount}",
                dto.FromAccountNumber, fromAccount.Balance, dto.Amount);
            throw new InsufficientFundsException(dto.FromAccountNumber, fromAccount.Balance, dto.Amount);
        }

        if (dto.FromAccountNumber == dto.ToAccountNumber)
        {
            _logger.LogWarning("Transfer failed: Cannot transfer to the same account {AccountNumber}", dto.FromAccountNumber);
            throw new InvalidTransactionException("Cannot transfer to the same account");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var fromBalanceBefore = fromAccount.Balance;
            fromAccount.Balance -= dto.Amount;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            fromAccount.UpdatedBy = createdBy;

            var debitTransaction = new Transaction
            {
                TransactionReference = GenerateTransactionReference(),
                AccountId = fromAccount.Id,
                Type = TransactionType.TransferOut,
                Amount = dto.Amount,
                BalanceBefore = fromBalanceBefore,
                BalanceAfter = fromAccount.Balance,
                Description = dto.Description,
                Status = TransactionStatus.Completed,
                RelatedAccountId = toAccount.Id,
                RelatedAccountNumber = toAccount.AccountNumber,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var toBalanceBefore = toAccount.Balance;
            toAccount.Balance += dto.Amount;
            toAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedBy = createdBy;

            var creditTransaction = new Transaction
            {
                TransactionReference = GenerateTransactionReference(),
                AccountId = toAccount.Id,
                Type = TransactionType.TransferIn,
                Amount = dto.Amount,
                BalanceBefore = toBalanceBefore,
                BalanceAfter = toAccount.Balance,
                Description = dto.Description,
                Status = TransactionStatus.Completed,
                RelatedAccountId = fromAccount.Id,
                RelatedAccountNumber = fromAccount.AccountNumber,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Accounts.Update(fromAccount);
            _unitOfWork.Accounts.Update(toAccount);
            await _unitOfWork.Transactions.AddAsync(debitTransaction);
            await _unitOfWork.Transactions.AddAsync(creditTransaction);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Transfer completed successfully: Debit {DebitRef}, Credit {CreditRef}, " +
                "From {FromAccount} (Balance: {FromBefore} -> {FromAfter}) to {ToAccount} (Balance: {ToBefore} -> {ToAfter}), Amount {Amount}",
                debitTransaction.TransactionReference, creditTransaction.TransactionReference,
                dto.FromAccountNumber, fromBalanceBefore, fromAccount.Balance,
                dto.ToAccountNumber, toBalanceBefore, toAccount.Balance, dto.Amount);

            await _auditService.LogAsync("Transfer", "Transaction",
                $"{debitTransaction.Id},{creditTransaction.Id}",
                null, $"Transferred {dto.Amount:C} from {dto.FromAccountNumber} to {dto.ToAccountNumber}",
                createdBy);

            var debitDto = _mapper.Map<TransactionDto>(debitTransaction);
            debitDto.AccountNumber = dto.FromAccountNumber;

            var creditDto = _mapper.Map<TransactionDto>(creditTransaction);
            creditDto.AccountNumber = dto.ToAccountNumber;

            InvalidateAccountCache(dto.FromAccountNumber);
            InvalidateAccountCache(dto.ToAccountNumber);
            InvalidateUserAccountsCache(fromAccount.UserId);
            InvalidateUserAccountsCache(toAccount.UserId);
            InvalidateAccountTransactionsCache(dto.FromAccountNumber);
            InvalidateAccountTransactionsCache(dto.ToAccountNumber);
            InvalidateDashboardCache(fromAccount.UserId);
            InvalidateDashboardCache(toAccount.UserId);

            return (debitDto, creditDto);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Concurrency conflict during transfer from {FromAccount} to {ToAccount}", 
                dto.FromAccountNumber, dto.ToAccountNumber);
            throw new InvalidOperationException("One of the accounts was modified by another transaction. Please try again.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Transfer transaction failed from {FromAccount} to {ToAccount}, amount {Amount}",
                dto.FromAccountNumber, dto.ToAccountNumber, dto.Amount);
            throw;
        }
    }

    public async Task<IEnumerable<TransactionDto>> GetAccountTransactionsAsync(
        string accountNumber, DateTime? from = null, DateTime? to = null)
    {
        var cacheKey = CacheKeys.AccountTransactions(accountNumber);

        if (!from.HasValue && !to.HasValue)
        {
            if (_cache.TryGetValue(cacheKey, out IEnumerable<TransactionDto>? cachedTransactions))
            {
                _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
                return cachedTransactions!;
            }
            _logger.LogDebug("Cache MISS: {CacheKey} - Retrieving from database", cacheKey);
        }

        var transactions = await _unitOfWork.Transactions
            .GetTransactionsByAccountNumberAsync(accountNumber, from, to);

        var result = _mapper.Map<IEnumerable<TransactionDto>>(transactions);

        foreach (var dto in result)
        {
            dto.AccountNumber = accountNumber;
        }

        if (!from.HasValue && !to.HasValue)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        }

        _logger.LogInformation("Retrieved {Count} transactions for account {AccountNumber}", result.Count(), accountNumber);

        return result;
    }

    public async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(DateTime? from = null, DateTime? to = null)
    {
        _logger.LogDebug("Retrieving all transactions, from {FromDate}, to {ToDate}", from, to);

        var transactions = await _unitOfWork.Transactions
            .GetAllTransactionsWithFiltersAsync(from, to);

        var result = _mapper.Map<IEnumerable<TransactionDto>>(transactions);

        _logger.LogInformation("Retrieved {Count} total transactions", result.Count());

        return result;
    }

    private void InvalidateAccountCache(string accountNumber)
    {
        var cacheKey = CacheKeys.Account(accountNumber);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
    }

    private void InvalidateUserAccountsCache(string userId)
    {
        var cacheKey = CacheKeys.UserAccounts(userId);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
    }

    private void InvalidateAccountTransactionsCache(string accountNumber)
    {
        var cacheKey = CacheKeys.AccountTransactions(accountNumber);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
    }

    private void InvalidateDashboardCache(string userId)
    {
        var cacheKey = CacheKeys.DashboardStats(userId);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
    }

    private static string GenerateTransactionReference()
    {
        return $"TBILISI{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
    }
}