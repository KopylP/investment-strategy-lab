namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record LayoutDto(IReadOnlyDictionary<string, NodePositionDto> Positions, IReadOnlyList<string> Collapsed, IReadOnlyDictionary<string, IReadOnlyList<NodePositionDto>>? EdgeRoutes = null);
