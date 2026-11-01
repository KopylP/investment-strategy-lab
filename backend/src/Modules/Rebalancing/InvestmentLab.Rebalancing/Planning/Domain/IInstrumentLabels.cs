namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IInstrumentLabels
{
    Task<IReadOnlyDictionary<Guid, string>> AllAsync(CancellationToken cancellationToken);
}
