using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Users.Application.Shared;

public interface IUserAccounts
{
    Task<Result> RegisterAsync(UserId userId, string email, string password, UserProfile profile, CancellationToken cancellationToken);

    Task<Result> SignInAsync(string email, string password, bool remember, CancellationToken cancellationToken);

    Task SignOutAsync();

    Task<CurrentUserDto?> FindAsync(UserId userId, CancellationToken cancellationToken);
}
