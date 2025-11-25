using Serilog;
using System.Diagnostics;
using System.Text;

namespace MyBankingSolution.Middleware;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        var stopwatch = Stopwatch.StartNew();

        await LogRequestAsync(context, requestId);

        var originalResponseBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            stopwatch.Stop();

            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;

            request.EnableBuffering();

            var requestBody = await ReadRequestBodyAsync(request);

            var requestLog = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path.ToString(),
                QueryString = request.QueryString.ToString(),
                ContentType = request.ContentType,
                UserAgent = request.Headers["User-Agent"].ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                User = context.User?.Identity?.Name ?? "Anonymous",
                Body = SanitizeRequestBody(requestBody, request.Path)
            };

            _logger.LogInformation(
                "HTTP Request [{RequestId}] {Method} {Path}{QueryString} by {User} from {IpAddress}",
                requestId,
                request.Method,
                request.Path,
                request.QueryString,
                requestLog.User,
                requestLog.RemoteIpAddress);

            Log.Debug("Request Details: {@RequestLog}", requestLog);

            request.Body.Position = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request for RequestId: {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
    {
        try
        {
            var response = context.Response;
            response.Body.Seek(0, SeekOrigin.Begin);

            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            var logLevel = response.StatusCode >= 500 ? LogLevel.Error
                         : response.StatusCode >= 400 ? LogLevel.Warning
                         : LogLevel.Information;

            var responseLog = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ElapsedMilliseconds = elapsedMs,
                Body = SanitizeResponseBody(responseBody, response.ContentType, response.StatusCode)
            };

            _logger.Log(
                logLevel,
                "HTTP Response [{RequestId}] {StatusCode} in {ElapsedMs}ms",
                requestId,
                response.StatusCode,
                elapsedMs);

            Log.Debug("Response Details: {@ResponseLog}", responseLog);

            if (elapsedMs > 1000)
            {
                _logger.LogWarning(
                    "SLOW REQUEST [{RequestId}] {Method} {Path} took {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response for RequestId: {RequestId}", requestId);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return string.Empty;

            if (!request.Body.CanSeek)
                return "[Body not readable - streaming]";

            request.Body.Position = 0;
            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return body;
        }
        catch
        {
            return "[Error reading body]";
        }
    }

    private static string SanitizeRequestBody(string body, PathString path)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        if (IsSensitiveEndpoint(path))
            return "[REDACTED - Sensitive]";

        if (body.Length > 5000)
            return $"{body[..5000]}... [TRUNCATED - {body.Length} bytes total]";

        return SanitizeSensitiveFields(body);
    }

    private static string SanitizeResponseBody(string? body, string? contentType, int statusCode)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        if (statusCode >= 500)
            return "[REDACTED - Server Error]";

        if (body.Length > 5000)
            return $"[TRUNCATED - {body.Length} bytes]";

        if (contentType?.Contains("image") == true ||
            contentType?.Contains("pdf") == true ||
            contentType?.Contains("octet-stream") == true)
            return "[Binary Content]";

        return body;
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var sensitivePaths = new[]
        {
            "/account/login",
            "/account/register",
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/token"
        };

        return sensitivePaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeSensitiveFields(string body)
    {
        try
        {
            var sensitivePatterns = new Dictionary<string, string>
            {
                { @"""password""\s*:\s*""[^""]*""", @"""password"": ""***""" },
                { @"""confirmPassword""\s*:\s*""[^""]*""", @"""confirmPassword"": ""***""" },
                { @"""pin""\s*:\s*""[^""]*""", @"""pin"": ""***""" },
                { @"""ssn""\s*:\s*""[^""]*""", @"""ssn"": ""***""" },
                { @"""creditCard""\s*:\s*""[^""]*""", @"""creditCard"": ""***""" },
                { @"""cvv""\s*:\s*""[^""]*""", @"""cvv"": ""***""" }
            };

            foreach (var pattern in sensitivePatterns)
            {
                body = System.Text.RegularExpressions.Regex.Replace(
                    body,
                    pattern.Key,
                    pattern.Value,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return body;
        }
        catch
        {
            return "[Error sanitizing body]";
        }
    }
}
