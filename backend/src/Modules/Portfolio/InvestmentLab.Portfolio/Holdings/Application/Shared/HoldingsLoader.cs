using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.Shared;

public sealed class HoldingsLoader(IHoldingsReader reader)
{
    public async Task<Result<(InvestmentAccount Account, AccountHoldings Holdings)>> LoadAsync(
        Guid accountId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await reader.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (asOf is null)
            return (account, await reader.GetCurrentAsync(account, cancellationToken));

        var projected = HoldingsProjector.Project(account, await reader.GetEntriesAsync(account.Id, cancellationToken), asOf);

        return projected.IsSuccess ? (account, projected.Value.Holdings) : projected.Error;
    }
}
