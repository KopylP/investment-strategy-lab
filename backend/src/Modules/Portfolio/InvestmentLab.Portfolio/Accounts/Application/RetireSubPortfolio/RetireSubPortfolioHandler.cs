using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;

public sealed record RetireSubPortfolioCommand(IReadOnlyDictionary<Guid, decimal> TargetSharesOfRemaining);

public sealed class RetireSubPortfolioHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result<AccountSetupDto>> Handle(
        Guid accountId,
        Guid subPortfolioId,
        RetireSubPortfolioCommand command,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (subPortfolioId == Guid.Empty)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId);

        var shares = TargetShares.TryFromPercents(command.TargetSharesOfRemaining);
        if (shares.IsFailure)
            return shares.Error;

        var retired = account.RetireSubPortfolio(SubPortfolioId.Of(subPortfolioId), shares.Value, clock.GetUtcNow());
        if (retired.IsFailure)
            return retired.Error;

        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
