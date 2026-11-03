namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IOwnSignals
{
    Task<IReadOnlyList<OwnSignal>> AllAsync(CancellationToken cancellationToken);
}
