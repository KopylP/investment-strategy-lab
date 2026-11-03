namespace InvestmentLab.Signals.Contracts;

public interface ISignalDirectory
{
    Task<IReadOnlyList<UserSignalDto>> GetSignalsAsync(CancellationToken cancellationToken);
}
