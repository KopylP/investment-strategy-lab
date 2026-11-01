namespace InvestmentLab.Signals.Contracts;

public interface IBuiltInSignalQueries
{
    Task<IReadOnlyList<BuiltInSignalDto>> GetBuiltInSignals(CancellationToken cancellationToken);
}
