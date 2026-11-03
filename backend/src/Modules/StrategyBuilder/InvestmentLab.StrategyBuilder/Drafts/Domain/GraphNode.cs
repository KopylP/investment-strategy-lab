namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record GraphNode(NodeId Id, NodeKind Kind, BlockKind Block, NodeParameters Parameters);
