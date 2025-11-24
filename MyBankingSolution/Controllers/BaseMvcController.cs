using Microsoft.AspNetCore.Mvc;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyBankingSolution.Controllers;

/// <summary>
/// Base controller for MVC controllers with common functionality
/// </summary>
public abstract class BaseMvcController : Controller
{
    protected readonly UserManager<ApplicationUser> UserManager;

    protected BaseMvcController(UserManager<ApplicationUser> userManager)
    {
        UserManager = userManager;
    }

    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    protected string? CurrentUserId => UserManager.GetUserId(User);

    /// <summary>
    /// Gets the current user's ID or redirects to login if not authenticated
    /// </summary>
    protected async Task<(bool success, string? userId)> TryGetCurrentUserIdAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return (false, null);
        }
        return (true, userId);
    }

    /// <summary>
    /// Checks if current user is authenticated and redirects to login if not
    /// </summary>
    protected IActionResult? RequireAuthentication()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return RedirectToAction("Login", "Account");
        }
        return null;
    }

    /// <summary>
    /// Sets a success message in TempData
    /// </summary>
    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Sets an error message in TempData
    /// </summary>
    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    /// <summary>
    /// Sets an info message in TempData
    /// </summary>
    protected void SetInfoMessage(string message)
    {
        TempData["InfoMessage"] = message;
    }

    /// <summary>
    /// Adds a model error and returns the view with the model
    /// </summary>
    protected IActionResult ModelErrorView<T>(string error, T model)
    {
        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    /// <summary>
    /// Handle exceptions and add to ModelState
    /// </summary>
    protected IActionResult HandleException(Exception ex, object? model = null)
    {
        ModelState.AddModelError(string.Empty, ex.Message);
        
        if (model != null)
            return View(model);
        
        return View();
    }
}
