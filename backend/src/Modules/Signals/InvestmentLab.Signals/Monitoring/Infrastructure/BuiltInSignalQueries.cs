using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class BuiltInSignalQueries(ISignalRepository signals) : IBuiltInSignalQueries
{
    public async Task<IReadOnlyList<BuiltInSignalDto>> GetBuiltInSignals(CancellationToken cancellationToken)
    {
        var all = await signals.AllAsync(cancellationToken);
        var builtIn = all.Where(signal => signal.IsBuiltIn);

        return [.. builtIn.Select(DtoOf)];
    }

    private static BuiltInSignalDto DtoOf(Signal signal) => new(
        SignalId: signal.Id,
        OwnerId: signal.OwnerId.Value,
        AccountId: signal.Target.AccountId,
        Kind: BuiltInSignalKind.OfKey(signal.BuiltInKey!));
}
