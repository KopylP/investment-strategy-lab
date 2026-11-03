namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record GraphDto(IReadOnlyList<NodeDto> Nodes, IReadOnlyList<LinkDto> Links);
