using Asp.Versioning;
using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MyBankingSolution.Controllers.Api.V1;

/// <summary>
/// Authentication and authorization endpoints.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="dto">Registration details</param>
    /// <returns>JWT token and user information</returns>
    /// <response code="201">User registered successfully</response>
    /// <response code="400">Invalid registration data</response>
    /// <response code="409">Email already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", dto.Email);

        if (!ModelState.IsValid)
            return ValidationErrorResponse();

        var (success, message, user) = await _authService.RegisterAsync(dto);

        if (!success)
        {
            _logger.LogWarning("Registration failed for email {Email}: {Message}", dto.Email, message);
            return ErrorResponse(message, StatusCodes.Status409Conflict);
        }

        var token = await _authService.GenerateJwtTokenAsync(user!);

        var response = new AuthResponse
        {
            Token = token,
            User = user!,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _logger.LogInformation("User registered successfully: {Email}", dto.Email);

        return CreatedResponse("GetUserProfile", new { userId = user!.Id }, response, "User registered successfully");
    }

    /// <summary>
    /// Authenticate user and get JWT token.
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid credentials</response>
    /// <response code="401">Authentication failed</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        if (!ModelState.IsValid)
            return ValidationErrorResponse();

        var (success, message, user) = await _authService.LoginAsync(dto);

        if (!success || user == null)
        {
            _logger.LogWarning("Login failed for email {Email}: {Message}", dto.Email, message);
            return ErrorResponse(message, StatusCodes.Status401Unauthorized);
        }

        var token = await _authService.GenerateJwtTokenAsync(user);

        var response = new AuthResponse
        {
            Token = token,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

        return SuccessResponse(response, "Login successful");
    }

    /// <summary>
    /// Get current user profile.
    /// </summary>
    /// <returns>User profile information</returns>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpGet("profile", Name = "GetUserProfile")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);

        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return ErrorResponse("User not found", StatusCodes.Status404NotFound);

        return SuccessResponse(user, "Profile retrieved successfully");
    }

    /// <summary>
    /// Refresh JWT token.
    /// </summary>
    /// <returns>New JWT token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("refresh-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return ErrorResponse("User not authenticated", StatusCodes.Status401Unauthorized);

        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return ErrorResponse("User not found", StatusCodes.Status404NotFound);

        var token = await _authService.GenerateJwtTokenAsync(user);

        var response = new AuthResponse
        {
            Token = token,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        return SuccessResponse(response, "Token refreshed successfully");
    }
}

/// <summary>
/// Authentication response model.
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
