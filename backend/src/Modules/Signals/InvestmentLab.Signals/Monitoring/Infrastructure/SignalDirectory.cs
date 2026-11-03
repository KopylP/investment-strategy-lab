using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class SignalDirectory(ISignalRepository signals) : ISignalDirectory
{
    public async Task<IReadOnlyList<UserSignalDto>> GetSignalsAsync(CancellationToken cancellationToken)
    {
        var all = await signals.AllAsync(cancellationToken);
        var userSignals = all.Where(signal => !signal.IsBuiltIn);

        return [.. userSignals.Select(ToContract)];
    }

    private static UserSignalDto ToContract(Signal signal) => new(
        Id: signal.Id,
        OwnerId: signal.OwnerId.Value,
        Name: signal.Name,
        Target: signal.Target.Kind.ToString(),
        InstrumentId: signal.Target.InstrumentId,
        GroupName: signal.Target.GroupName,
        AccountId: signal.Target.AccountId,
        Metric: signal.Condition.Metric.ToString(),
        Threshold: signal.Condition.Threshold,
        IsMarketSignal: signal.Target.IsMarket);
}
