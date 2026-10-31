namespace InvestmentLab.Portfolio.Contracts.Holdings;

public sealed record PositionDto(Guid InstrumentId, decimal Quantity, decimal CostBasis, decimal AverageCost, DateOnly FirstOpenedOn, IReadOnlyList<LotDto> Lots);
