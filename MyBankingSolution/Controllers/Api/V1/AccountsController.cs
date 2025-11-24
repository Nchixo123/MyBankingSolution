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
[EnableRateLimiting("api")]
public class AccountsController(IAccountService accountService, ILogger<AccountsController> logger) : BaseApiController
{
    private readonly IAccountService _accountService = accountService;
    private readonly ILogger<AccountsController> _logger = logger;

    [HttpGet("my-accounts")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAccounts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        pageSize = Math.Min(pageSize, 100);

        var allAccounts = await _accountService.GetUserAccountsAsync(userId);
        var totalCount = allAccounts.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedAccounts = allAccounts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var result = new PagedResult<AccountDto>
        {
            Items = pagedAccounts,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved page {PageNumber} of {TotalPages} accounts for user {UserId} (Total: {TotalCount})",
            pageNumber, totalPages, userId, totalCount);

        return SuccessResponse(result, $"Retrieved {pagedAccounts.Count()} of {totalCount} accounts");
    }

    [HttpGet("{accountNumber}", Name = "GetAccountByNumber")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(string accountNumber)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        var account = await _accountService.GetAccountByNumberAsync(accountNumber);

        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountNumber}", accountNumber);
            return ErrorResponse("Account not found", StatusCodes.Status404NotFound);
        }

        if (account.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to account {AccountNumber} by user {UserId}",
                accountNumber, userId);
            return ErrorResponse("Not authorized to access this account", StatusCodes.Status403Forbidden);
        }

        return SuccessResponse(account, "Account retrieved successfully");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
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

        try
        {
            dto.UserId = userId;
            var account = await _accountService.CreateAccountAsync(dto, userId);

            _logger.LogInformation("Account created successfully: {AccountNumber} for user {UserId}",
                account.AccountNumber, userId);

            return CreatedResponse(
                "GetAccountByNumber",
                new { accountNumber = account.AccountNumber },
                account,
                "Account created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for user {UserId}", userId);
            return ErrorResponse(ex.Message, StatusCodes.Status409Conflict);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllAccounts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Min(pageSize, 100);

        var allAccounts = await _accountService.GetAllAccountsAsync();
        var totalCount = allAccounts.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedAccounts = allAccounts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var result = new PagedResult<AccountDto>
        {
            Items = pagedAccounts,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved page {PageNumber} of {TotalPages} for all accounts (Total: {TotalCount})",
            pageNumber, totalPages, totalCount);

        return SuccessResponse(result, $"Retrieved {pagedAccounts.Count()} of {totalCount} accounts");
    }

    [HttpPatch("{accountNumber}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccountStatus(
        string accountNumber,
        [FromBody] UpdateAccountStatusRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        try
        {
            var account = await _accountService.GetAccountByNumberAsync(accountNumber);
            if (account == null)
            {
                return ErrorResponse("Account not found", StatusCodes.Status404NotFound);
            }

            var success = await _accountService.UpdateAccountStatusAsync(account.Id, request.Status, userId);

            if (!success)
            {
                return ErrorResponse("Failed to update account status", StatusCodes.Status400BadRequest);
            }

            var updatedAccount = await _accountService.GetAccountByNumberAsync(accountNumber);

            _logger.LogInformation("Account {AccountNumber} status updated to {Status} by user {UserId}",
                accountNumber, request.Status, userId);

            return SuccessResponse(updatedAccount, "Account status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account status for {AccountNumber}", accountNumber);
            return ErrorResponse(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public class UpdateAccountStatusRequest
{
    public BankingSystem.Domain.Entities.Enums.AccountStatus Status { get; set; }
}