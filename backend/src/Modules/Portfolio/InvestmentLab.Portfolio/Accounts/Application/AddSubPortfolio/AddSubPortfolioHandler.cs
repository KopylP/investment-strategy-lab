using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;

public sealed record AddSubPortfolioCommand(SubPortfolioInput SubPortfolio, IReadOnlyDictionary<Guid, decimal> TargetSharesOfOthers);

public sealed class AddSubPortfolioHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, AddSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var draft = AccountInput.ToDraft(command.SubPortfolio);
        if (draft.IsFailure)
            return draft.Error;

        var shares = AccountInput.ToShares(command.TargetSharesOfOthers);
        if (shares.IsFailure)
            return shares.Error;

        var added = account.AddSubPortfolio(draft.Value, shares.Value, clock.GetUtcNow());
        if (added.IsFailure)
            return added.Error;

        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
