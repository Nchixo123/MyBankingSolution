using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using MyBankingSolution.Extensions;

namespace MyBankingSolution.Controllers;

[Authorize]
[EnableRateLimiting("transaction")]
public class TransactionController(
    IAccountService accountService,
    ITransactionService transactionService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly IAccountService _accountService = accountService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet]
    [ResponseCache(Duration = 30, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Deposit()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var accounts = await _accountService.GetUserAccountsAsync(userId);
        
        if (!accounts.Any())
        {
            this.AddWarningToast("You need to create an account first before making a deposit.");
            return RedirectToAction("Index", "BankAccount");
        }

        ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Deposit(DepositDto model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            this.AddErrorToast("Please correct the errors in the form.");
            return View(model);
        }

        try
        {
            await _transactionService.DepositAsync(model, userId);
            this.AddSuccessToast($"Successfully deposited {model.Amount:C} to account {model.AccountNumber}!");
            TempData["SuccessMessage"] = $"Successfully deposited {model.Amount:C}";
            return RedirectToAction("Details", "BankAccount", new { accountNumber = model.AccountNumber });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            this.AddErrorToast($"Deposit failed: {ex.Message}");
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            return View(model);
        }
    }

    [HttpGet]
    [ResponseCache(Duration = 30, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Withdraw()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var accounts = await _accountService.GetUserAccountsAsync(userId);
        
        if (!accounts.Any())
        {
            this.AddWarningToast("You need to create an account first before making a withdrawal.");
            return RedirectToAction("Index", "BankAccount");
        }

        ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Withdraw(WithdrawalDto model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            this.AddErrorToast("Please correct the errors in the form.");
            return View(model);
        }

        try
        {
            await _transactionService.WithdrawAsync(model, userId);
            this.AddSuccessToast($"Successfully withdrew {model.Amount:C} from account {model.AccountNumber}!");
            TempData["SuccessMessage"] = $"Successfully withdrew {model.Amount:C}";
            return RedirectToAction("Details", "BankAccount", new { accountNumber = model.AccountNumber });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            this.AddErrorToast($"Withdrawal failed: {ex.Message}");
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            return View(model);
        }
    }

    [HttpGet]
    [ResponseCache(Duration = 30, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Transfer()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var accounts = await _accountService.GetUserAccountsAsync(userId);
        
        if (!accounts.Any())
        {
            this.AddWarningToast("You need to create an account first before making a transfer.");
            return RedirectToAction("Index", "BankAccount");
        }

        if (accounts.Count() < 2)
        {
            this.AddWarningToast("You need at least 2 accounts to make a transfer.");
            return RedirectToAction("Index", "BankAccount");
        }

        ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Transfer(TransferDto model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            this.AddErrorToast("Please correct the errors in the form.");
            return View(model);
        }

        try
        {
            await _transactionService.TransferAsync(model, userId);
            this.AddSuccessToast($"Successfully transferred {model.Amount:C} from {model.FromAccountNumber} to {model.ToAccountNumber}!");
            TempData["SuccessMessage"] = $"Successfully transferred {model.Amount:C} to {model.ToAccountNumber}";
            return RedirectToAction("Details", "BankAccount", new { accountNumber = model.FromAccountNumber });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            this.AddErrorToast($"Transfer failed: {ex.Message}");
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            ViewBag.Accounts = new SelectList(accounts, "AccountNumber", "AccountNumber");
            return View(model);
        }
    }

    [HttpGet]
    [DisableRateLimiting]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "accountNumber" }, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> History(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber))
            return BadRequest();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var account = await _accountService.GetAccountByNumberAsync(accountNumber);
        if (account == null)
        {
            this.AddErrorToast("Account not found.");
            return NotFound();
        }

        if (account.UserId != userId && !User.IsInRole("Admin"))
        {
            this.AddWarningToast("You don't have permission to view this account's transaction history.");
            return Forbid();
        }

        var transactions = await _transactionService.GetAccountTransactionsAsync(accountNumber);
        ViewBag.AccountNumber = accountNumber;

        return View(transactions);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    [EnableRateLimiting("api")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> All()
    {
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return View(transactions);
    }
}
