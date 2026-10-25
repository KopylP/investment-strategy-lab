using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Shared.Infrastructure;
using InvestmentLab.Identity.Users.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class UserAccounts(
    IdentityDbContext db,
    UserStore store,
    UserManager<User> users,
    SignInManager<User> signIn,
    TimeProvider clock) : IUserAccounts
{
    public async Task<Result> RegisterAsync(
        UserId userId, string email, string password, UserProfile profile, UserRegisteredV1 registered, CancellationToken cancellationToken)
    {
        // UserManager validates and hashes; the store only stages the user so profile and event commit with it.
        store.AutoSaveChanges = false;
        try
        {
            var created = await users.CreateAsync(new User { Id = userId.Value, UserName = email, Email = email }, password);
            if (!created.Succeeded)
                return UserErrors.Rejected(ToSnakeCase(created.Errors.First().Code), string.Join(" ", created.Errors.Select(e => e.Description)));

            db.UserProfiles.Add(profile);
            db.Publish(registered, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        finally
        {
            store.AutoSaveChanges = true;
        }
    }

    public async Task<Result> SignInAsync(string email, string password, bool remember, CancellationToken cancellationToken)
    {
        var result = await signIn.PasswordSignInAsync(email, password, remember, lockoutOnFailure: true);
        return result switch
        {
            { Succeeded: true } => Result.Success(),
            { IsLockedOut: true } => UserErrors.LockedOut(),
            _ => UserErrors.InvalidCredentials(),
        };
    }

    public Task SignOutAsync() => signIn.SignOutAsync();

    public async Task<CurrentUserDto?> FindAsync(UserId userId, CancellationToken cancellationToken) =>
        await db.Users.Where(user => user.Id == userId.Value)
            .Select(user => new CurrentUserDto(user.Id, user.Email!))
            .SingleOrDefaultAsync(cancellationToken);

    private static string ToSnakeCase(string code) =>
        string.Concat(code.Select((c, i) => char.IsUpper(c) && i > 0 ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));
}
