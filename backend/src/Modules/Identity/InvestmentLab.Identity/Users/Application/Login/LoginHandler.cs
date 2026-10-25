namespace InvestmentLab.Identity.Users.Application.Login;

public sealed record LoginCommand(string Email, string Password, bool Remember = true);

public sealed class LoginHandler(IUserAccounts accounts)
{
    public Task<Result> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        accounts.SignInAsync(command.Email.Trim(), command.Password, command.Remember, cancellationToken);
}
