namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftChange(int Revision, string? Name, string? Description, DraftSetup? Setup, StrategyGraph? Graph, IReadOnlyList<DraftReport>? Reports);
