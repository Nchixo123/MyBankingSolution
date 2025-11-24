using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBankingSolution.Extensions;

namespace MyBankingSolution.Controllers.Api;

/// <summary>
/// Base controller for all API controllers with common functionality.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Returns a standardized success response.
    /// </summary>
    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Request completed successfully",
            Data = data
        });
    }

    /// <summary>
    /// Returns a standardized created response.
    /// </summary>
    protected IActionResult CreatedResponse<T>(string routeName, object routeValues, T data, string? message = null)
    {
        return CreatedAtRoute(routeName, routeValues, new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Resource created successfully",
            Data = data
        });
    }

    /// <summary>
    /// Returns a standardized error response.
    /// </summary>
    protected IActionResult ErrorResponse(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        });
    }

    /// <summary>
    /// Returns a standardized validation error response using extension method.
    /// </summary>
    protected IActionResult ValidationErrorResponse()
    {
        var errors = ModelState.GetModelStateErrors();
        return BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        });
    }

    /// <summary>
    /// Returns a standardized validation error response with custom errors.
    /// </summary>
    protected IActionResult ValidationErrorResponse(Dictionary<string, string[]> errors)
    {
        return BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        });
    }
}

/// <summary>
/// Standardized API response wrapper.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// Standardized API validation error response.
/// </summary>
public class ApiValidationErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
}

/// <summary>
/// Paged result wrapper for list endpoints.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
