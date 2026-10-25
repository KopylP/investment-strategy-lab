using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;

/// <param name="TargetSharesOfOthers">New shares (percent) of every other active sub-portfolio.</param>
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

        var added = account.AddSubPortfolio(draft.Value, shares.Value);
        if (added.IsFailure)
            return added.Error;

        var now = clock.GetUtcNow();
        accounts.Publish(new SubPortfolioAddedV1(account.Id.Value, added.Value.Value, draft.Value.Name.Trim()), now);
        accounts.Publish(account.ToSharesChanged(now), now);
        await accounts.SaveChangesAsync(cancellationToken);
        return account.ToSetupDto();
    }
}
