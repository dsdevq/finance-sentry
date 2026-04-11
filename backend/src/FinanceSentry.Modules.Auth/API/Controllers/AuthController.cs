using FinanceSentry.Modules.Auth.Application.Commands;
using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

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
            var response = await _mediator.Send(new LoginCommand(request.Email, request.Password));
            return Ok(response);
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
            var response = await _mediator.Send(new RegisterCommand(request.Email, request.Password));
            return Created(string.Empty, response);
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
}
