using System.Text;

namespace Mkat.Api.Middleware;

public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicAuthMiddleware> _logger;

    public BasicAuthMiddleware(RequestDelegate next, ILogger<BasicAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        // Peer pairing protocol endpoints use secret-based auth, not Basic Auth
        if (path.Equals("/api/v1/peers/pair/accept", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/v1/peers/pair/unpair", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"mkat\"");
            return;
        }

        try
        {
            var authValue = authHeader.ToString();
            if (!authValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var encodedCredentials = authValue["Basic ".Length..].Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var username = parts[0];
            var password = parts[1];

            var expectedUsername = Environment.GetEnvironmentVariable("MKAT_USERNAME") ?? "admin";
            var expectedPassword = Environment.GetEnvironmentVariable("MKAT_PASSWORD");

            if (string.IsNullOrEmpty(expectedPassword))
            {
                _logger.LogWarning("MKAT_PASSWORD environment variable not set");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Server misconfigured" });
                return;
            }

            if (username != expectedUsername || password != expectedPassword)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", username);
                context.Response.StatusCode = 401;
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authentication");
            context.Response.StatusCode = 401;
        }
    }
}
