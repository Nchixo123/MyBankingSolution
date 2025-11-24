using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MyBankingSolution.Controllers;

[Authorize]
public class DashboardController(
    IAccountService accountService,
    ITransactionService transactionService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly IAccountService _accountService = accountService;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [ResponseCache(Duration = 30, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var accounts = await _accountService.GetUserAccountsAsync(userId);
        return View(accounts);
    }
}
