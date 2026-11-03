namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IStrategyAuthoring
{
    Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken);
}
