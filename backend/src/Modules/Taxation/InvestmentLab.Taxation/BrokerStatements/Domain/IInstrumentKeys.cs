namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public interface IInstrumentKeys
{
    Task<IReadOnlyDictionary<Guid, InstrumentKey>> AllAsync(CancellationToken cancellationToken);
}
