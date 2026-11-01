namespace InvestmentLab.Signals.Monitoring.Domain;

public interface IOpenPositions
{
    Task<IReadOnlyList<IOpenPosition>?> OfAsync(Guid accountId, CancellationToken cancellationToken);
}
