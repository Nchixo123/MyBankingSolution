using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyBankingSolution.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    IAuthService authService,
    IAccountService accountService,
    ITransactionService transactionService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IAccountService _accountService = accountService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _authService.GetAllUsersAsync();
        var accounts = await _accountService.GetAllAccountsAsync();
        var transactions = await _transactionService.GetAllTransactionsAsync();

        ViewBag.TotalUsers = users.Count();
        ViewBag.TotalAccounts = accounts.Count();
        ViewBag.TotalTransactions = transactions.Count();
        ViewBag.TotalBalance = accounts.Sum(a => a.Balance);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _authService.GetAllUsersAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> ManageRoles(string userId)
    {
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        ViewBag.AvailableRoles = new SelectList(new[] { "Admin", "Staff", "Customer" });
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var (success, message) = await _authService.AssignRoleAsync(userId, role);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Accounts()
    {
        var accounts = await _accountService.GetAllAccountsAsync();
        return View(accounts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccountStatus(int accountId, AccountStatus status)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _accountService.UpdateAccountStatusAsync(accountId, status, userId);

        if (result)
            TempData["SuccessMessage"] = "Account status updated successfully";
        else
            TempData["ErrorMessage"] = "Failed to update account status";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public async Task<IActionResult> Transactions()
    {
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return View(transactions);
    }
}
