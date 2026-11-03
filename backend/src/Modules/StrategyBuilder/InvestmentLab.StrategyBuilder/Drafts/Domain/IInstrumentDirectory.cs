namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IInstrumentDirectory
{
    Task<IReadOnlyList<DirectoryInstrument>> AllAsync(CancellationToken cancellationToken);
}
