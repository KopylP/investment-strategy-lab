using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.EnsureBuiltInSignals;

public sealed class EnsureBuiltInSignalsHandler(ISignalRepository signals, IAccountSetupQueries accounts, IInstrumentCatalog instruments) : IHandler
{
    private const int MaxAccountNameLength = 50;

    public async Task Handle(CancellationToken cancellationToken)
    {
        var setups = await accounts.GetAccountSetups(cancellationToken);
        if (setups.Count == 0)
            return;

        var vixId = await FindVixAsync(cancellationToken);
        var knownKeys = await signals.BuiltInKeysAsync(cancellationToken);

        var missing = MissingSignals(setups, vixId, knownKeys);
        if (missing.Count == 0)
            return;

        foreach (var signal in missing)
            signals.Add(signal);

        await signals.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> FindVixAsync(CancellationToken cancellationToken)
    {
        var catalog = await instruments.GetInstruments(cancellationToken);
        var vix = catalog.FirstOrDefault(instrument => instrument.IsVolatilityIndex);

        return vix?.Id;
    }

    private static List<Signal> MissingSignals(IReadOnlyList<AccountSetupDto> setups, Guid? vixId, IReadOnlySet<string> knownKeys)
    {
        var wanted = setups.SelectMany(account => WantedFor(account, vixId));

        return [.. wanted
            .DistinctBy(signal => signal.BuiltInKey)
            .Where(signal => !knownKeys.Contains(signal.BuiltInKey!))];
    }

    private static IEnumerable<Signal> WantedFor(AccountSetupDto account, Guid? vixId)
    {
        var owner = UserId.Of(account.OwnerId);

        if (vixId is { } instrumentId)
            yield return HighVix(owner, instrumentId);

        yield return AccountFall(owner, account);
        yield return PositionFall(owner, account);
    }

    private static Signal HighVix(UserId owner, Guid vixId)
    {
        var key = BuiltInSignalKind.VolatilityIndexKey(owner.Value);
        var target = SignalTarget.Instrument(vixId);
        var condition = BuiltInSignalRules.ConditionOf(BuiltInSignalKind.VolatilityIndex);

        return Signal.Create(owner, Message.Of("signal.builtin.high_vix"), target, condition, Cadence.Off, key).Value;
    }

    private static Signal AccountFall(UserId owner, AccountSetupDto account)
    {
        var key = BuiltInSignalKind.AccountDrawdownKey(account.Id);
        var name = Message.Of("signal.builtin.account_drawdown", new { account = ShortName(account) });
        var target = SignalTarget.Account(account.Id);
        var condition = BuiltInSignalRules.ConditionOf(BuiltInSignalKind.AccountDrawdown);

        return Signal.Create(owner, name, target, condition, Cadence.Off, key).Value;
    }

    private static Signal PositionFall(UserId owner, AccountSetupDto account)
    {
        var key = BuiltInSignalKind.PositionDrawdownKey(account.Id);
        var name = Message.Of("signal.builtin.position_drawdown", new { account = ShortName(account) });
        var target = SignalTarget.EachPosition(account.Id);
        var condition = BuiltInSignalRules.ConditionOf(BuiltInSignalKind.PositionDrawdown);

        return Signal.Create(owner, name, target, condition, Cadence.Off, key).Value;
    }

    private static string ShortName(AccountSetupDto account) =>
        account.Name.Length <= MaxAccountNameLength ? account.Name : account.Name[..MaxAccountNameLength];
}
