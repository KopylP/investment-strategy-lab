namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SubjectReadings(IReadOnlyList<SubjectReading> Measured, IReadOnlySet<string> LiveSubjects)
{
    public static readonly SubjectReadings Nothing = new([], new HashSet<string>());

    public static SubjectReadings Unmeasured(string subjectKey) =>
        new([], new HashSet<string> { subjectKey });

    public static SubjectReadings One(SubjectReading reading) =>
        new([reading], new HashSet<string> { reading.Key });
}
