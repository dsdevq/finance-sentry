using FinanceSentry.Modules.Auth.Application.DTOs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

        var token = _tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        return new AuthResponse(token, expiresAt, user.Id);
    }
}
