using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class PositionSubjectReader(
    IOpenPositions openPositions,
    IAccountSetupQueries accounts,
    IInstrumentLabels instrumentLabels,
    IFxRates fxRates,
    QuoteReader quotes)
{
    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var accountId = signal.Target.AccountId!.Value;
        var account = await accounts.GetAccountSetup(accountId, cancellationToken);
        var held = await openPositions.OfAsync(accountId, cancellationToken);
        if (account is null || held is null)
            return SubjectReadings.Nothing;

        var labels = await instrumentLabels.AllAsync(cancellationToken);
        var positions = HeldPositionsOf(held, labels);

        var measured = await MeasureAllAsync(positions, signal.Condition, account.BaseCurrency, cancellationToken);
        var live = positions.Select(position => position.Subject.Key).ToHashSet();

        return new SubjectReadings(measured, live);
    }

    private static List<HeldPosition> HeldPositionsOf(IReadOnlyList<IOpenPosition> held, IReadOnlyDictionary<Guid, string> labels)
    {
        var merged = MergedPosition.AllOf(held);

        return [.. merged.Select(position => HeldPositionOf(position, labels))];
    }

    private static HeldPosition HeldPositionOf(MergedPosition position, IReadOnlyDictionary<Guid, string> labels)
    {
        var key = InstrumentSubjects.KeyOf(position.InstrumentId);
        var label = InstrumentSubjects.LabelOf(labels, position.InstrumentId);

        return new HeldPosition(new QuoteSubject(position.InstrumentId, key, label), position.AverageCost, position.FirstOpenedOn);
    }

    private async Task<List<SubjectReading>> MeasureAllAsync(
        List<HeldPosition> positions, SignalCondition condition, string accountCurrency, CancellationToken cancellationToken)
    {
        var measured = new List<SubjectReading>();

        foreach (var position in positions)
        {
            var reading = await MeasureAsync(position, condition, accountCurrency, cancellationToken);
            if (reading is not null)
                measured.Add(reading);
        }

        return measured;
    }

    private Task<SubjectReading?> MeasureAsync(
        HeldPosition position, SignalCondition condition, string accountCurrency, CancellationToken cancellationToken)
    {
        if (condition.Metric == SignalMetric.LossFromCost)
            return MeasureLossAsync(position, accountCurrency, cancellationToken);

        var from = WindowStartSincePurchase(position, condition);

        return quotes.MeasureAsync(position.Subject, condition.Metric, from, cancellationToken);
    }

    private DateOnly WindowStartSincePurchase(HeldPosition position, SignalCondition condition)
    {
        var windowStart = MeasurementWindow.StartOf(condition, quotes.Today);

        if (condition.Metric != SignalMetric.DrawdownFromPeak)
            return windowStart;

        return windowStart > position.FirstOpenedOn ? windowStart : position.FirstOpenedOn;
    }

    private async Task<SubjectReading?> MeasureLossAsync(HeldPosition position, string accountCurrency, CancellationToken cancellationToken)
    {
        var instrumentId = position.Subject.InstrumentId;

        var latest = await quotes.LatestFreshPriceAsync(instrumentId, cancellationToken);
        if (latest is null || await quotes.IsSpikeOnAsync(instrumentId, latest.Date, cancellationToken))
            return null;

        var rate = await fxRates.GetCrossRate(latest.Currency, accountCurrency, latest.Date, cancellationToken);
        if (rate is not { } knownRate)
            return null;

        var loss = SignalMetrics.LossFromCost(latest.Close * knownRate, position.AverageCost);

        return position.Subject.ReadingOf(loss, latest.Date);
    }
}
