using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Accounts.Application.RenameSubPortfolio;

public sealed record RenameSubPortfolioCommand(string Name);

public sealed class RenameSubPortfolioHandler(IAccountRepository accounts) : IHandler
{
    public async Task<Result<AccountSetupDto>> Handle(
        Guid accountId,
        Guid subPortfolioId,
        RenameSubPortfolioCommand command,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (subPortfolioId == Guid.Empty)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId);

        var renamed = account.RenameSubPortfolio(SubPortfolioId.Of(subPortfolioId), command.Name);
        if (renamed.IsFailure)
            return renamed.Error;

        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
