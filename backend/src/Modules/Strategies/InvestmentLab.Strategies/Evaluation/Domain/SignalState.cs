namespace InvestmentLab.Strategies.Evaluation.Domain;

public sealed record SignalState(Guid SignalId, IReadOnlyList<Guid> ActiveFor);
