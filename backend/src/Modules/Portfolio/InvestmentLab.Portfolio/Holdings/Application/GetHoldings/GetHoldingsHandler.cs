using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.GetHoldings;

public sealed class GetHoldingsHandler(IHoldingsReader reader)
{
    public async Task<Result<AccountHoldingsDto>> Handle(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await reader.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (asOf is null)
            return (await reader.GetCurrentAsync(account, cancellationToken)).ToDto(account);

        var projected = HoldingsProjector.Project(account, await reader.GetEntriesAsync(account.Id, cancellationToken), asOf);

        return projected.IsSuccess ? projected.Value.Holdings.ToDto(account) : projected.Error;
    }
}
