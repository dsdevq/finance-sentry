using FinanceSentry.Modules.Auth.Application.Commands;
using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSentry.Modules.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    private const string RefreshTokenCookie = "fs_refresh_token";

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
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
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
            SetRefreshTokenCookie(result.RawRefreshToken);
            return Ok(result.Response);
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
            var result = await mediator.Send(new RegisterCommand(request.Email, request.Password));
            SetRefreshTokenCookie(result.RawRefreshToken);
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
            var result = await mediator.Send(new RefreshCommand(rawToken));
            SetRefreshTokenCookie(result.RawRefreshToken);
            return Ok(result.Response);
        }
        catch (UnauthorizedAccessException)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { error = "Refresh token invalid or expired.", errorCode = "INVALID_REFRESH_TOKEN" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
            await mediator.Send(new LogoutCommand(userId));

        DeleteRefreshTokenCookie();
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
}
