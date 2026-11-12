using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Application.Shared;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.GetValuedHoldings;

public sealed record ValuedHoldingsDto(
    Guid AccountId,
    string Currency,
    DateOnly? AsOf,
    decimal Value,
    decimal Cash,
    decimal CostBasis,
    decimal Result,
    decimal? ResultShare,
    IReadOnlyList<ValuedSubPortfolioDto> SubPortfolios);

public sealed record ValuedSubPortfolioDto(
    Guid SubPortfolioId,
    string Name,
    decimal Cash,
    IReadOnlyList<CashBalanceDto> CashBalances,
    decimal Value,
    decimal CostBasis,
    decimal Result,
    decimal Share,
    IReadOnlyList<ValuedPositionDto> Positions);

public sealed record ValuedPositionDto(
    Guid InstrumentId,
    decimal Quantity,
    decimal AverageCost,
    decimal CostBasis,
    decimal? Price,
    decimal? QuotedPrice,
    string? QuoteCurrency,
    DateOnly? PriceDate,
    bool IsStale,
    decimal Value,
    decimal Result,
    decimal? ResultShare,
    decimal Weight,
    IReadOnlyList<LotDto> Lots);

public sealed class GetValuedHoldingsHandler(HoldingsLoader loader, IHoldingsMarket market, TimeProvider clock) : IHandler
{
    public async Task<Result<ValuedHoldingsDto>> Handle(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, asOf, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (account, holdings) = loaded.Value;
        var day = asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var prices = await market.OnAsync(holdings, day, cancellationToken);

        var valued = HoldingsValuation.Of(holdings, prices);

        return valued.ToDto(account);
    }
}
