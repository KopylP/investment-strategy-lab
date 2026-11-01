using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class OpenPositions(IHoldingsQueries holdings) : IOpenPositions
{
    public async Task<IReadOnlyList<IOpenPosition>?> OfAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var held = await holdings.GetHoldings(accountId, null, cancellationToken);
        if (held is null)
            return null;

        var positions = held.SubPortfolios.SelectMany(subPortfolio => subPortfolio.Positions);

        return [.. positions.Select(position => new OpenPosition(position))];
    }

    private sealed class OpenPosition(PositionDto dto) : IOpenPosition
    {
        private readonly PositionDto _dto = dto;

        public Guid InstrumentId => _dto.InstrumentId;
        public decimal Quantity => _dto.Quantity;
        public decimal CostBasis => _dto.CostBasis;
        public DateOnly FirstOpenedOn => _dto.FirstOpenedOn;
    }
}
