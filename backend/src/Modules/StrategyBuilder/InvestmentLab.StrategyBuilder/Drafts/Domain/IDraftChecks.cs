namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IDraftChecks
{
    Task<Result<DraftCheck>> CheckAsync(StrategyDraft draft, CancellationToken cancellationToken);
}
