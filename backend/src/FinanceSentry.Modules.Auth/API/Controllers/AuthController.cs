using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.Commands;
using FinanceSentry.Modules.Auth.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.Auth.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    ICommandHandler<LoginCommand, AuthResult> loginHandler,
    ICommandHandler<RegisterCommand, AuthResult> registerHandler,
    ICommandHandler<RefreshCommand, AuthResult> refreshHandler,
    ICommandHandler<VerifyGoogleCredentialCommand, AuthResult> googleVerifyHandler,
    ICommandHandler<LogoutCommand, Unit> logoutHandler,
    IQueryHandler<GetMeQuery, GetMeResult> getMeHandler) : ControllerBase
{
    private const string RefreshTokenCookie = "fs_refresh_token";
    private const string AccessTokenCookie = "fs_access_token";

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(new { error = "No session found.", errorCode = "INVALID_REFRESH_TOKEN" });

        try
        {
            var result = await getMeHandler.Handle(new GetMeQuery(rawToken), HttpContext.RequestAborted);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (UnauthorizedAccessException)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { error = "Session expired. Please sign in again.", errorCode = "INVALID_REFRESH_TOKEN" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        try
        {
            var result = await loginHandler.Handle(new LoginCommand(request.Email, request.Password), HttpContext.RequestAborted);
            SetRefreshTokenCookie(result.RawRefreshToken);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new
            {
                error = "Validation failed.",
                errorCode = "VALIDATION_ERROR",
                details = ex.Errors.Select(e => e.ErrorMessage),
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "GOOGLE_ACCOUNT_ONLY")
        {
            return Unauthorized(new
            {
                error = "This account uses Google sign-in. Please use 'Continue with Google'.",
                errorCode = "GOOGLE_ACCOUNT_ONLY"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                error = "Invalid email or password.",
                errorCode = "INVALID_CREDENTIALS"
            });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                error = "Validation failed.",
                errorCode = "VALIDATION_ERROR",
                details = new[]
                {
                    string.IsNullOrWhiteSpace(request.Email) ? "Email is required." : null,
                    string.IsNullOrWhiteSpace(request.Password) ? "Password is required." : null
                }.Where(d => d is not null)
            });
        }

        try
        {
            var result = await registerHandler.Handle(new RegisterCommand(request.Email, request.Password), HttpContext.RequestAborted);
            SetRefreshTokenCookie(result.RawRefreshToken);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Created(string.Empty, result.Response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_EMAIL")
        {
            return BadRequest(new
            {
                error = "Email is already registered.",
                errorCode = "DUPLICATE_EMAIL"
            });
        }
        catch (ArgumentException ex) when (ex.Message.StartsWith("VALIDATION_ERROR:"))
        {
            var details = ex.Message["VALIDATION_ERROR:".Length..].Split('|');
            return BadRequest(new
            {
                error = "Validation failed.",
                errorCode = "VALIDATION_ERROR",
                details
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(new { error = "Refresh token missing.", errorCode = "INVALID_REFRESH_TOKEN" });

        try
        {
            var result = await refreshHandler.Handle(new RefreshCommand(rawToken), HttpContext.RequestAborted);
            SetRefreshTokenCookie(result.RawRefreshToken);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (UnauthorizedAccessException)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { error = "Refresh token invalid or expired.", errorCode = "INVALID_REFRESH_TOKEN" });
        }
    }

    [HttpPost("google/verify")]
    public async Task<IActionResult> GoogleVerify([FromBody] VerifyGoogleCredentialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
            return BadRequest(new { error = "Credential is required.", errorCode = "VALIDATION_ERROR" });

        try
        {
            var result = await googleVerifyHandler.Handle(new VerifyGoogleCredentialCommand(request.Credential), HttpContext.RequestAborted);
            SetRefreshTokenCookie(result.RawRefreshToken);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_GOOGLE_CREDENTIAL")
        {
            return BadRequest(new { error = "Invalid Google credential.", errorCode = "INVALID_GOOGLE_CREDENTIAL" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
            await logoutHandler.Handle(new LogoutCommand(userId), HttpContext.RequestAborted);

        DeleteRefreshTokenCookie();
        DeleteAccessTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string rawToken)
    {
        Response.Cookies.Append(RefreshTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/"
        });
    }

    private void SetAccessTokenCookie(string rawToken, DateTime expiresAt)
    {
        Response.Cookies.Append(AccessTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = new DateTimeOffset(expiresAt, TimeSpan.Zero),
            Path = "/"
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private void DeleteAccessTokenCookie()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }
}

public record VerifyGoogleCredentialRequest(string Credential);
