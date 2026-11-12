using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Application.Shared;

namespace InvestmentLab.Portfolio.Holdings.Application.GetHoldings;

public sealed class GetHoldingsHandler(HoldingsLoader loader)
{
    public async Task<Result<AccountHoldingsDto>> Handle(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, asOf, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (account, holdings) = loaded.Value;

        return holdings.ToDto(account);
    }
}
