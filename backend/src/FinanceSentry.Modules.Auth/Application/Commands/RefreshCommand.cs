using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.DTOs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

/// <param name="RawRefreshToken">The raw refresh token value read from the httpOnly cookie.</param>
public record RefreshCommand(string RawRefreshToken) : ICommand<AuthResult>;
