using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class InstrumentSubjectReader(IInstrumentLabels instrumentLabels, QuoteReader quotes)
{
    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var instrumentId = signal.Target.InstrumentId!.Value;
        var key = InstrumentSubjects.KeyOf(instrumentId);
        var labels = await instrumentLabels.AllAsync(cancellationToken);
        var subject = new QuoteSubject(instrumentId, key, InstrumentSubjects.LabelOf(labels, instrumentId));

        var from = MeasurementWindow.StartOf(signal.Condition, quotes.Today);
        var reading = await quotes.MeasureAsync(subject, signal.Condition.Metric, from, cancellationToken);

        return reading is null ? SubjectReadings.Unmeasured(key) : SubjectReadings.One(reading);
    }
}
