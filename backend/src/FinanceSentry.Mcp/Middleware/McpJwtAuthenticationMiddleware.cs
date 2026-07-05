using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FinanceSentry.Core.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSentry.Mcp.Middleware;

public sealed class McpJwtAuthenticationMiddleware
{
    private const string AccessTokenCookie = "fs_access_token";

    private readonly RequestDelegate _next;
    private readonly ILogger<McpJwtAuthenticationMiddleware> _logger;
    private readonly TokenValidationParameters _validationParams;

    public McpJwtAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<McpJwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        _validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ReadToken(context);
        if (string.IsNullOrWhiteSpace(token))
        {
            await WriteUnauthorizedAsync(context, "Authentication required.", "UNAUTHORIZED");
            return;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParams, out _);
            context.User = principal;
            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Expired MCP HTTP JWT token received.");
            await WriteUnauthorizedAsync(context, "Token has expired. Refresh the session and try again.", "TOKEN_EXPIRED");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Invalid MCP HTTP JWT token: {Message}", ex.Message);
            await WriteUnauthorizedAsync(context, "Invalid authentication token.", "TOKEN_INVALID");
        }
    }

    private static string? ReadToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return authorization[prefix.Length..].Trim();

        return context.Request.Cookies[AccessTokenCookie];
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsJsonAsync(new ApiErrorBody(message, code));
    }
}
