namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftProblemDto(string Message, string Severity, string? NodeId, string? Block, string? ReportCode);
