using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;

namespace InvestmentLab.Portfolio.Holdings.Infrastructure;

internal sealed class HoldingsQueries(GetHoldingsHandler getHoldings) : IHoldingsQueries
{
    public async Task<AccountHoldingsDto?> GetHoldings(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken) =>
        await getHoldings.Handle(accountId, asOf, cancellationToken) is { IsSuccess: true } found ? found.Value : null;

    public async Task<SubPortfolioHoldingsDto?> GetSubPortfolioSnapshot(Guid accountId, Guid subPortfolioId, DateOnly? asOf, CancellationToken cancellationToken) =>
        (await GetHoldings(accountId, asOf, cancellationToken))?.SubPortfolios.FirstOrDefault(sub => sub.SubPortfolioId == subPortfolioId);
}
