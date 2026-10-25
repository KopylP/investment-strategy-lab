using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Users.Application;

/// <summary>Credentials and sign-in, backed by ASP.NET Core Identity.</summary>
public interface IUserAccounts
{
    /// <summary>Creates the user, their profile and <see cref="UserRegisteredV1"/> in one transaction.</summary>
    Task<Result> RegisterAsync(UserId userId, string email, string password, UserProfile profile, UserRegisteredV1 registered, CancellationToken cancellationToken);

    Task<Result> SignInAsync(string email, string password, bool remember, CancellationToken cancellationToken);

    Task SignOutAsync();

    Task<CurrentUserDto?> FindAsync(UserId userId, CancellationToken cancellationToken);
}

public sealed record CurrentUserDto(Guid Id, string Email);

public static class UserErrors
{
    public static Error InvalidCredentials() => Error.Validation("user.invalid_credentials", "Email or password is wrong.");

    public static Error LockedOut() => Error.Forbidden("user.locked_out", "Too many failed attempts; try again in a few minutes.");

    public static Error Rejected(string code, string message) => Error.Validation($"user.{code}", message);

    public static Error NotFound(UserId userId) => Error.NotFound("user.not_found", $"User {userId} does not exist.");
}
