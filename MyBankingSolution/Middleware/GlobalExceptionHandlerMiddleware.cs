using System.Net;
using System.Text.Json;
using BankingSystem.Application.Exceptions;
using Serilog;

namespace MyBankingSolution.Middleware;

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred: {Message} | Path: {Path} | Method: {Method} | User: {User}", 
                ex.Message, 
                context.Request.Path, 
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous");
            
            await HandleExceptionAsync(context, ex, GetOptions());
        }
    }

    private JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, JsonSerializerOptions options)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "An error occurred while processing your request.",
            Details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        switch (exception)
        {
            // Custom Banking Exceptions
            case AccountNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = ex.Message;
                response.Details = _env.IsDevelopment() ? $"Account Number: {ex.AccountNumber}" : null;
                Log.Warning("Account not found: {AccountNumber}", ex.AccountNumber);
                break;

            case AccountInactiveException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.Details = _env.IsDevelopment() ? $"Account Number: {ex.AccountNumber}" : null;
                Log.Warning("Inactive account access attempt: {AccountNumber}", ex.AccountNumber);
                break;

            case InsufficientFundsException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.Details = _env.IsDevelopment() 
                    ? $"Account: {ex.AccountNumber}, Balance: {ex.Balance:C}, Requested: {ex.RequestedAmount:C}" 
                    : null;
                Log.Warning("Insufficient funds: Account {AccountNumber}, Balance: {Balance}, Requested: {RequestedAmount}",
                    ex.AccountNumber, ex.Balance, ex.RequestedAmount);
                break;

            case DuplicateAccountException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = ex.Message;
                response.Details = _env.IsDevelopment() ? $"Account Number: {ex.AccountNumber}" : null;
                Log.Warning("Duplicate account attempt: {AccountNumber}", ex.AccountNumber);
                break;

            case InvalidTransactionException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                Log.Warning("Invalid transaction: {Message}", ex.Message);
                break;

            case BankingException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                Log.Warning("Banking exception: {Message}", ex.Message);
                break;

            // Standard Exceptions
            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access.";
                Log.Warning("Unauthorized access attempt");
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found.";
                Log.Warning("Resource not found: {Message}", exception.Message);
                break;

            case ArgumentException:
            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                Log.Warning("Bad request: {Message}", exception.Message);
                break;

            case ApplicationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                Log.Warning("Application exception: {Message}", exception.Message);
                break;

            default:
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    response.Message = _env.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred.";
                    Log.Error(exception, "Unhandled API exception");
                }
                else
                {
                    context.Response.Redirect("/Home/Error");
                    return;
                }
                break;
        }

        context.Response.StatusCode = response.StatusCode;
        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
