using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class PositionSubjectReader(
    IHoldingsQueries holdings,
    IAccountSetupQueries accounts,
    IInstrumentLabels instrumentLabels,
    IFxRates fxRates,
    QuoteReader quotes)
{
    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var accountId = signal.Target.AccountId!.Value;
        var account = await accounts.GetAccountSetup(accountId, cancellationToken);
        var held = await holdings.GetHoldings(accountId, null, cancellationToken);
        if (account is null || held is null)
            return SubjectReadings.Nothing;

        var labels = await instrumentLabels.AllAsync(cancellationToken);
        var positions = HeldPositionsOf(held, labels);

        var measured = await MeasureAllAsync(positions, signal.Condition, account.BaseCurrency, cancellationToken);
        var live = positions.Select(position => position.Subject.Key).ToHashSet();

        return new SubjectReadings(measured, live);
    }

    private static List<HeldPosition> HeldPositionsOf(AccountHoldingsDto held, IReadOnlyDictionary<Guid, string> labels)
    {
        var open = held.SubPortfolios
            .SelectMany(subPortfolio => subPortfolio.Positions)
            .Where(position => position.Quantity > 0m);

        return [.. open
            .GroupBy(position => position.InstrumentId)
            .Select(parts => Merged(parts, labels))];
    }

    private static HeldPosition Merged(IGrouping<Guid, PositionDto> parts, IReadOnlyDictionary<Guid, string> labels)
    {
        var quantity = parts.Sum(part => part.Quantity);
        var cost = parts.Sum(part => part.CostBasis);
        var firstOpenedOn = parts.Min(part => part.FirstOpenedOn);
        var subject = new QuoteSubject(parts.Key, InstrumentSubjects.KeyOf(parts.Key), InstrumentSubjects.LabelOf(labels, parts.Key));

        return new HeldPosition(subject, cost / quantity, firstOpenedOn);
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
