namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineSignalState(string Id, IReadOnlyList<string> ActiveFor);
