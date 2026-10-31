namespace InvestmentLab.Signals.Monitoring.Domain;

public interface ISignalRepository
{
    Task<Signal?> FindAsync(Guid signalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Signal>> AllAsync(CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> BuiltInKeysAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalOccurrence>> OpenOccurrencesOfAsync(Guid signalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalOccurrence>> AllOpenOccurrencesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalOccurrence>> ClearedOccurrencesAsync(int count, CancellationToken cancellationToken);

    void Add(Signal signal);

    void Add(SignalOccurrence occurrence);

    void Remove(Signal signal);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
