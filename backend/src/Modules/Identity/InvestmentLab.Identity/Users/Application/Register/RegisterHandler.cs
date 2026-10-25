using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Users.Application.Register;

public sealed record RegisterCommand(string Email, string Password);

public sealed class RegisterHandler(IUserAccounts accounts, TimeProvider clock)
{
    public async Task<Result<CurrentUserDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var userId = UserId.New();
        var email = command.Email.Trim();

        var registered = await accounts.RegisterAsync(
            userId, email, command.Password, UserProfile.CreateDefault(userId),
            new UserRegisteredV1(userId.Value, email, clock.GetUtcNow()), cancellationToken);

        return registered.IsSuccess ? new CurrentUserDto(userId.Value, email) : registered.Error;
    }
}
