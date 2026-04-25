using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.Commands;
using FinanceSentry.Modules.Auth.Domain.Exceptions;
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
            throw new InvalidRefreshTokenException("No session found.");

        try
        {
            var result = await getMeHandler.Handle(new GetMeQuery(rawToken), HttpContext.RequestAborted);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (InvalidRefreshTokenException)
        {
            DeleteRefreshTokenCookie();
            throw;
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        var result = await loginHandler.Handle(new LoginCommand(request.Email, request.Password), HttpContext.RequestAborted);
        SetRefreshTokenCookie(result.RawRefreshToken);
        SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
        return Ok(result.Response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        var result = await registerHandler.Handle(new RegisterCommand(request.Email, request.Password), HttpContext.RequestAborted);
        SetRefreshTokenCookie(result.RawRefreshToken);
        SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
        return Created(string.Empty, result.Response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new InvalidRefreshTokenException("Refresh token missing.");

        try
        {
            var result = await refreshHandler.Handle(new RefreshCommand(rawToken), HttpContext.RequestAborted);
            SetRefreshTokenCookie(result.RawRefreshToken);
            SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
            return Ok(result.Response);
        }
        catch (InvalidRefreshTokenException)
        {
            DeleteRefreshTokenCookie();
            throw;
        }
    }

    [HttpPost("google/verify")]
    public async Task<IActionResult> GoogleVerify([FromBody] VerifyGoogleCredentialRequest request)
    {
        var result = await googleVerifyHandler.Handle(new VerifyGoogleCredentialCommand(request.Credential), HttpContext.RequestAborted);
        SetRefreshTokenCookie(result.RawRefreshToken);
        SetAccessTokenCookie(result.RawAccessToken, result.Response.ExpiresAt);
        return Ok(result.Response);
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
