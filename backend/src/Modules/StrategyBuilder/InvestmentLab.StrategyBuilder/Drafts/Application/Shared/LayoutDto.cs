using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record LayoutDto(IReadOnlyDictionary<string, NodePosition> Positions, IReadOnlyList<string> Collapsed, IReadOnlyDictionary<string, IReadOnlyList<NodePosition>>? EdgeRoutes = null);
