namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IRuleVocabulary
{
    Task<Result<RuleVocabulary>> GetAsync(CancellationToken cancellationToken);
}
