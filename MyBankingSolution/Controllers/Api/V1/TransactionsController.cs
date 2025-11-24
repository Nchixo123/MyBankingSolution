using Asp.Versioning;
using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MyBankingSolution.Controllers.Api.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("transaction")]
public class TransactionsController(
    ITransactionService transactionService,
    IAccountService accountService,
    ILogger<TransactionsController> logger) : BaseApiController
{
    private readonly ITransactionService _transactionService = transactionService;
    private readonly IAccountService _accountService = accountService;
    private readonly ILogger<TransactionsController> _logger = logger;

    [HttpPost("deposit")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit([FromBody] DepositDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            return ValidationErrorResponse(errors);
        }

        var account = await _accountService.GetAccountByNumberAsync(dto.AccountNumber);
        if (account == null)
        {
            return ErrorResponse("Account not found", StatusCodes.Status404NotFound);
        }

        if (account.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized deposit attempt to account {AccountNumber} by user {UserId}",
                dto.AccountNumber, userId);
            return ErrorResponse("Not authorized to deposit to this account", StatusCodes.Status403Forbidden);
        }

        try
        {
            var transaction = await _transactionService.DepositAsync(dto, userId);

            _logger.LogInformation("Deposit successful: {TransactionRef}, Account {AccountNumber}, Amount {Amount}",
                transaction.TransactionReference, dto.AccountNumber, dto.Amount);

            return CreatedResponse(
                "GetTransaction",
                new { transactionReference = transaction.TransactionReference },
                transaction,
                "Deposit successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deposit failed for account {AccountNumber}, amount {Amount}",
                dto.AccountNumber, dto.Amount);
            return ErrorResponse(ex.Message, StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            return ValidationErrorResponse(errors);
        }

        var account = await _accountService.GetAccountByNumberAsync(dto.AccountNumber);
        if (account == null)
        {
            return ErrorResponse("Account not found", StatusCodes.Status404NotFound);
        }

        if (account.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized withdrawal attempt from account {AccountNumber} by user {UserId}",
                dto.AccountNumber, userId);
            return ErrorResponse("Not authorized to withdraw from this account", StatusCodes.Status403Forbidden);
        }

        try
        {
            var transaction = await _transactionService.WithdrawAsync(dto, userId);

            _logger.LogInformation("Withdrawal successful: {TransactionRef}, Account {AccountNumber}, Amount {Amount}",
                transaction.TransactionReference, dto.AccountNumber, dto.Amount);

            return CreatedResponse(
                "GetTransaction",
                new { transactionReference = transaction.TransactionReference },
                transaction,
                "Withdrawal successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Withdrawal failed for account {AccountNumber}, amount {Amount}",
                dto.AccountNumber, dto.Amount);
            return ErrorResponse(ex.Message, StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(ApiResponse<TransferResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            return ValidationErrorResponse(errors);
        }

        var fromAccount = await _accountService.GetAccountByNumberAsync(dto.FromAccountNumber);
        if (fromAccount == null)
        {
            return ErrorResponse("Source account not found", StatusCodes.Status404NotFound);
        }

        if (fromAccount.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized transfer attempt from account {AccountNumber} by user {UserId}",
                dto.FromAccountNumber, userId);
            return ErrorResponse("Not authorized to transfer from this account", StatusCodes.Status403Forbidden);
        }

        try
        {
            var (debitTransaction, creditTransaction) = await _transactionService.TransferAsync(dto, userId);

            _logger.LogInformation("Transfer successful: Debit {DebitRef}, Credit {CreditRef}, " +
                "From {FromAccount} to {ToAccount}, Amount {Amount}",
                debitTransaction.TransactionReference, creditTransaction.TransactionReference,
                dto.FromAccountNumber, dto.ToAccountNumber, dto.Amount);

            var response = new TransferResponse
            {
                DebitTransaction = debitTransaction,
                CreditTransaction = creditTransaction
            };

            return CreatedAtRoute("GetTransaction",
                new { transactionReference = debitTransaction.TransactionReference },
                new ApiResponse<TransferResponse>
                {
                    Success = true,
                    Message = "Transfer successful",
                    Data = response
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed from {FromAccount} to {ToAccount}, amount {Amount}",
                dto.FromAccountNumber, dto.ToAccountNumber, dto.Amount);
            return ErrorResponse(ex.Message, StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("{transactionReference}", Name = "GetTransaction")]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(string transactionReference)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        var allTransactions = await _transactionService.GetAllTransactionsAsync();
        var transaction = allTransactions.FirstOrDefault(t => t.TransactionReference == transactionReference);

        if (transaction == null)
        {
            return ErrorResponse("Transaction not found", StatusCodes.Status404NotFound);
        }

        // Verify user has access to this transaction
        var account = await _accountService.GetAccountByNumberAsync(transaction.AccountNumber);
        if (account != null && account.UserId != userId && !User.IsInRole("Admin"))
        {
            return ErrorResponse("Not authorized to view this transaction", StatusCodes.Status403Forbidden);
        }

        return SuccessResponse(transaction, "Transaction retrieved successfully");
    }

    [HttpGet("account/{accountNumber}")]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountTransactions(
        string accountNumber,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        // Verify user owns the account
        var account = await _accountService.GetAccountByNumberAsync(accountNumber);
        if (account == null)
        {
            return ErrorResponse("Account not found", StatusCodes.Status404NotFound);
        }

        if (account.UserId != userId && !User.IsInRole("Admin"))
        {
            return ErrorResponse("Not authorized to view transactions for this account", StatusCodes.Status403Forbidden);
        }

        var transactions = await _transactionService.GetAccountTransactionsAsync(accountNumber, from, to);

        _logger.LogInformation("Retrieved {Count} transactions for account {AccountNumber}",
            transactions.Count(), accountNumber);

        return SuccessResponse(transactions, $"Retrieved {transactions.Count()} transactions");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TransactionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Min(pageSize, 100);

        var allTransactions = await _transactionService.GetAllTransactionsAsync(from, to);
        var totalCount = allTransactions.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedTransactions = allTransactions
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var result = new PagedResult<TransactionDto>
        {
            Items = pagedTransactions,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved page {PageNumber} of {TotalPages} for all transactions (Total: {TotalCount})",
            pageNumber, totalPages, totalCount);

        return SuccessResponse(result, $"Retrieved {pagedTransactions.Count()} of {totalCount} transactions");
    }
}
public class TransferResponse
{
    public TransactionDto DebitTransaction { get; set; } = null!;
    public TransactionDto CreditTransaction { get; set; } = null!;
}
