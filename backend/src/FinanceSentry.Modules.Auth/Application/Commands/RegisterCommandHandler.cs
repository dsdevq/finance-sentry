using FinanceSentry.Modules.Auth.Application.DTOs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException("DUPLICATE_EMAIL");

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new ArgumentException("VALIDATION_ERROR:" + string.Join("|", result.Errors.Select(e => e.Description)));

        var token = _tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        return new AuthResponse(token, expiresAt, user.Id);
    }
}
