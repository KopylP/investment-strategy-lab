using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class InstrumentGroupSubjectReader(IInstrumentGroups groups, QuoteReader quotes)
{
    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var members = await groups.MembersOfAsync(signal.Target.GroupName!, cancellationToken);
        var subjects = members.Select(SubjectOf).ToList();

        var from = MeasurementWindow.StartOf(signal.Condition, quotes.Today);
        var measured = await MeasureAllAsync(subjects, signal.Condition.Metric, from, cancellationToken);
        var live = subjects.Select(subject => subject.Key).ToHashSet();

        return new SubjectReadings(measured, live);
    }

    private static QuoteSubject SubjectOf(GroupMember member) =>
        new(member.InstrumentId, InstrumentSubjects.KeyOf(member.InstrumentId), member.Label);

    private async Task<List<SubjectReading>> MeasureAllAsync(
        List<QuoteSubject> subjects, SignalMetric metric, DateOnly from, CancellationToken cancellationToken)
    {
        var measured = new List<SubjectReading>();

        foreach (var subject in subjects)
        {
            var reading = await quotes.MeasureAsync(subject, metric, from, cancellationToken);
            if (reading is not null)
                measured.Add(reading);
        }

        return measured;
    }
}
