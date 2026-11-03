namespace InvestmentLab.Strategies.Contracts;

public sealed record SignalStateDto(Guid SignalId, IReadOnlyList<Guid> ActiveFor);
