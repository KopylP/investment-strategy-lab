namespace InvestmentLab.Strategies.Contracts;

public sealed record ParametersCheckDto(IReadOnlyList<ParameterProblemDto> Problems, IReadOnlyList<Guid> SignalIds, int HistoryBars = 260);
