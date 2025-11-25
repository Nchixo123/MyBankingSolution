using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MyBankingSolution.Controllers;

public abstract class BaseMvcController(UserManager<ApplicationUser> userManager) : Controller
{
    protected readonly UserManager<ApplicationUser> UserManager = userManager;

    protected string? CurrentUserId => UserManager.GetUserId(User);

    protected async Task<(bool success, string? userId)> TryGetCurrentUserIdAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return (false, null);
        }
        return (true, userId);
    }
    protected IActionResult? RequireAuthentication()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return RedirectToAction("Login", "Account");
        }
        return null;
    }

    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    protected void SetInfoMessage(string message)
    {
        TempData["InfoMessage"] = message;
    }

    protected IActionResult ModelErrorView<T>(string error, T model)
    {
        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    protected IActionResult HandleException(Exception ex, object? model = null)
    {
        ModelState.AddModelError(string.Empty, ex.Message);

        if (model != null)
            return View(model);

        return View();
    }
}
