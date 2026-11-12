using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Users.Application.Shared;

namespace InvestmentLab.Identity.Users.Application.Register;

public sealed record RegisterCommand(string Email, string Password);

public sealed class RegisterHandler(IUserAccounts accounts, TimeProvider clock) : IHandler
{
    public async Task<Result<CurrentUserDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var userId = UserId.New();
        var email = command.Email.Trim();

        var profile = UserProfile.CreateDefault(userId);
        profile.Registered(email, clock.GetUtcNow());

        var registered = await accounts.RegisterAsync(userId, email, command.Password, profile, cancellationToken);

        return registered.IsSuccess ? new CurrentUserDto(userId.Value, email) : registered.Error;
    }
}
