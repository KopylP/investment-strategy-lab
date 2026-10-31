using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed record QuoteSubject(Guid InstrumentId, string Key, string Label)
{
    public SubjectReading? ReadingOf(decimal? value, DateOnly dataDate) =>
        value is { } measured ? new SubjectReading(Key, Label, measured, dataDate) : null;
}
