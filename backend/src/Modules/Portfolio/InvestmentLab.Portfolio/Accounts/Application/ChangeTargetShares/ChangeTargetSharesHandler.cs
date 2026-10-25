using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;

/// <param name="TargetShares">Sub-portfolio id → percent, for every active sub-portfolio.</param>
public sealed record ChangeTargetSharesCommand(IReadOnlyDictionary<Guid, decimal> TargetShares);

public sealed class ChangeTargetSharesHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, ChangeTargetSharesCommand command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var shares = AccountInput.ToShares(command.TargetShares);
        if (shares.IsFailure)
            return shares.Error;

        var changed = account.ChangeTargetShares(shares.Value);
        if (changed.IsFailure)
            return changed.Error;

        var now = clock.GetUtcNow();
        accounts.Publish(account.ToSharesChanged(now), now);
        await accounts.SaveChangesAsync(cancellationToken);
        return account.ToSetupDto();
    }
}
