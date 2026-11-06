namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record GraphLink(NodeId From, NodeId To, string Input, string Output = "output")
{
    public string RouteKey() => string.Join("|", new[] { From.Value, Output, To.Value, Input }.Select(value => $"{value.Length}:{value}"));
}
