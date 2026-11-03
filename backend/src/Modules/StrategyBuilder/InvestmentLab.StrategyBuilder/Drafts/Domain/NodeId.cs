namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record NodeId(string Value)
{
    public override string ToString() => Value;
}
