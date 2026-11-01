namespace InvestmentLab.Signals.Monitoring.Domain;

public interface IInstrumentLabels
{
    Task<IReadOnlyDictionary<Guid, string>> AllAsync(CancellationToken cancellationToken);
}
