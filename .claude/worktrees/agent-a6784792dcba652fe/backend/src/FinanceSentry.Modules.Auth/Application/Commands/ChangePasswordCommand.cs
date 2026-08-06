using FinanceSentry.Core.Cqrs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : ICommand<Unit>;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
