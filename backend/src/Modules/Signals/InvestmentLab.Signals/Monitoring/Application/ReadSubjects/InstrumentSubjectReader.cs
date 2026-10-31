using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class InstrumentSubjectReader(IInstrumentCatalog instruments, QuoteReader quotes)
{
    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var instrumentId = signal.Target.InstrumentId!.Value;
        var key = InstrumentLabels.KeyOf(instrumentId);
        var catalog = await instruments.GetInstruments(cancellationToken);
        var subject = new QuoteSubject(instrumentId, key, InstrumentLabels.LabelOf(catalog, instrumentId));

        var from = MeasurementWindow.StartOf(signal.Condition, quotes.Today);
        var reading = await quotes.MeasureAsync(subject, signal.Condition.Metric, from, cancellationToken);

        return reading is null ? SubjectReadings.Unmeasured(key) : SubjectReadings.One(reading);
    }
}
