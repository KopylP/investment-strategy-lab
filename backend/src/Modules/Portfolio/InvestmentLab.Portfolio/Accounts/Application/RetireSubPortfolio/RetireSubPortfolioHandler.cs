using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;

/// <param name="TargetSharesOfRemaining">New shares (percent) of the sub-portfolios that stay active.</param>
public sealed record RetireSubPortfolioCommand(IReadOnlyDictionary<Guid, decimal> TargetSharesOfRemaining);

public sealed class RetireSubPortfolioHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, Guid subPortfolioId, RetireSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);
        if (subPortfolioId == Guid.Empty)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId);

        var shares = AccountInput.ToShares(command.TargetSharesOfRemaining);
        if (shares.IsFailure)
            return shares.Error;

        var retired = account.RetireSubPortfolio(SubPortfolioId.Of(subPortfolioId), shares.Value);
        if (retired.IsFailure)
            return retired.Error;

        var now = clock.GetUtcNow();
        accounts.Publish(account.ToSharesChanged(now), now);
        await accounts.SaveChangesAsync(cancellationToken);
        return account.ToSetupDto();
    }
}
