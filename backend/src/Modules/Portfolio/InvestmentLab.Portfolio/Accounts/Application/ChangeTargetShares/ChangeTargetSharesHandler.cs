using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;

public sealed record ChangeTargetSharesCommand(IReadOnlyDictionary<Guid, decimal> TargetShares);

public sealed class ChangeTargetSharesHandler(IAccountRepository accounts, TimeProvider clock) : IHandler
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, ChangeTargetSharesCommand command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var shares = TargetShares.TryFromPercents(command.TargetShares);
        if (shares.IsFailure)
            return shares.Error;

        var changed = account.ChangeTargetShares(shares.Value, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
