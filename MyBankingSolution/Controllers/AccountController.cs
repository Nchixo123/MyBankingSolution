using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyBankingSolution.Extensions;

namespace MyBankingSolution.Controllers;

[EnableRateLimiting("auth")]
public class AccountController(
    IAuthService authService,
    IAccountService accountService,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : BaseMvcController(userManager)
{
    private readonly IAuthService _authService = authService;
    private readonly IAccountService _accountService = accountService;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    [HttpGet]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await UserManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            this.AddErrorToast("Invalid email or password. Please try again.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            TempData["ToastSuccess"] = $"Welcome back, {user.FirstName}! You have successfully logged in.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts.");
            this.AddErrorToast("Your account has been locked due to multiple failed login attempts. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        this.AddErrorToast("Invalid email or password. Please check your credentials.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            this.AddErrorToast("Please correct the errors in the form before submitting.");
            return View(model);
        }

        var (success, message, user) = await _authService.RegisterAsync(model);

        if (!success)
        {
            this.AddErrorToast(message);
            return ModelErrorView(message, model);
        }

        TempData["ToastSuccess"] = $"Registration successful, {model.FirstName}! Please login with your credentials.";
        TempData["SuccessMessage"] = "Account created successfully! Please login to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [DisableRateLimiting]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        
        TempData["ToastInfo"] = "You have been logged out successfully. See you soon!";
        TempData["InfoMessage"] = "You have been logged out successfully.";
        
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult AccessDenied()
    {
        this.AddWarningToast("Access denied. You don't have permission to access this resource.");
        return View();
    }

    [Authorize]
    [HttpGet]
    [DisableRateLimiting]
    public async Task<IActionResult> MyAccounts()
    {
        var authCheck = RequireAuthentication();
        if (authCheck != null) return authCheck;

        try
        {
            var accounts = await _accountService.GetUserAccountsAsync(CurrentUserId!);
            return View(accounts);
        }
        catch (Exception ex)
        {
            this.AddErrorToast("Failed to load your accounts. Please try again.");
            SetErrorMessage("Failed to load accounts: " + ex.Message);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
