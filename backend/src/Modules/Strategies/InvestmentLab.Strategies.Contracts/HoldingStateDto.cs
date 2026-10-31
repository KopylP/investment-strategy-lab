namespace InvestmentLab.Strategies.Contracts;

public sealed record HoldingStateDto(Guid InstrumentId, IReadOnlyList<LotStateDto> Lots);
