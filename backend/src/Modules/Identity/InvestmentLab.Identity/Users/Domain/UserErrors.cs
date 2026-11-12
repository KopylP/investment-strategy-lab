namespace InvestmentLab.Identity.Users.Domain;

public static class UserErrors
{
    public static Error InvalidCredentials() => Error.Validation("user.invalid_credentials", "Email or password is wrong.");

    public static Error LockedOut() => Error.Forbidden("user.locked_out", "Too many failed attempts; try again in a few minutes.");

    public static Error Rejected(string code, string message) => Error.Validation($"user.{code}", message);

    public static Error NotFound(UserId userId) => Error.NotFound("user.not_found", $"User {userId} does not exist.");
}
