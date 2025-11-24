using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyBankingSolution.Extensions;

namespace MyBankingSolution.Controllers;

[Authorize]
public class BankAccountController : BaseMvcController
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;

    public BankAccountController(
        IAccountService accountService,
        ITransactionService transactionService,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _accountService = accountService;
        _transactionService = transactionService;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index()
    {
        var authCheck = RequireAuthentication();
        if (authCheck != null) return authCheck;

        var accounts = await _accountService.GetUserAccountsAsync(CurrentUserId!);
        return View(accounts);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.AccountTypes = ViewBagHelpers.GetAccountTypesSelectList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create(CreateAccountDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AccountTypes = ViewBagHelpers.GetAccountTypesSelectList();
            this.AddErrorToast("Please correct the errors in the form.");
            return View(model);
        }

        try
        {
            var authCheck = RequireAuthentication();
            if (authCheck != null) return authCheck;

            model.UserId = CurrentUserId!;
            var account = await _accountService.CreateAccountAsync(model, CurrentUserId!);

            this.AddSuccessToast($"Account created successfully! Your new account number is {account.AccountNumber}.");
            SetSuccessMessage("Account created successfully!");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ViewBag.AccountTypes = ViewBagHelpers.GetAccountTypesSelectList();
            this.AddErrorToast($"Failed to create account: {ex.Message}");
            return HandleException(ex, model);
        }
    }

    [HttpGet]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "accountNumber" }, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Details(string accountNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(accountNumber))
            {
                this.AddErrorToast("Account number is required.");
                return RedirectToAction(nameof(Index));
            }

            var account = await _accountService.GetAccountByNumberAsync(accountNumber);
            if (account == null)
            {
                this.AddErrorToast($"Account {accountNumber} not found.");
                SetErrorMessage("Account not found.");
                return RedirectToAction(nameof(Index));
            }

            if (account.UserId != CurrentUserId && !User.IsInRole("Admin"))
            {
                this.AddWarningToast("You don't have permission to view this account.");
                SetErrorMessage("You don't have permission to view this account.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Accounts = await _accountService.GetUserAccountsAsync(CurrentUserId!);
            return View(account);
        }
        catch (Exception ex)
        {
            this.AddErrorToast($"Error loading account: {ex.Message}");
            SetErrorMessage($"Error loading account: {ex.Message}");
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> All()
    {
        var accounts = await _accountService.GetAllAccountsAsync();
        return View(accounts);
    }
}
