namespace InvestmentLab.Portfolio.Contracts.Holdings;

/// <summary>What sub-portfolios hold, now or as of a past date.</summary>
public interface IHoldingsQueries
{
    /// <param name="asOf">Null: the current holdings.</param>
    Task<AccountHoldingsDto?> GetHoldings(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken);

    Task<SubPortfolioHoldingsDto?> GetSubPortfolioSnapshot(Guid accountId, Guid subPortfolioId, DateOnly? asOf, CancellationToken cancellationToken);
}

/// <param name="AsOf">Date of the last entry taken into account (or the requested date).</param>
public sealed record AccountHoldingsDto(Guid AccountId, string Currency, DateOnly? AsOf, IReadOnlyList<SubPortfolioHoldingsDto> SubPortfolios);

/// <param name="Cash">May be negative when a buy was recorded before its contribution.</param>
public sealed record SubPortfolioHoldingsDto(Guid SubPortfolioId, string Name, decimal Cash, IReadOnlyList<PositionDto> Positions);

/// <param name="AverageCost">Cost per unit, fees included.</param>
public sealed record PositionDto(Guid InstrumentId, decimal Quantity, decimal CostBasis, decimal AverageCost, DateOnly FirstOpenedOn, IReadOnlyList<LotDto> Lots);

public sealed record LotDto(Guid OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
