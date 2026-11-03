namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed class TrialCleanup : AggregateRoot<Guid>
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromDays(1);
    private const int MaximumBackoffExponent = 9;

    public Guid TrialStrategyId { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }

    private TrialCleanup(Guid id) : base(id) { }

    public static TrialCleanup For(Guid trialStrategyId, DateTimeOffset now) => new(Guid.CreateVersion7())
    {
        TrialStrategyId = trialStrategyId,
        NextAttemptAt = now,
    };

    public void Failed(DateTimeOffset now)
    {
        Attempts++;
        var exponent = Math.Min(Attempts, MaximumBackoffExponent);
        var seconds = InitialDelay.TotalSeconds * Math.Pow(2, exponent);
        var delay = TimeSpan.FromSeconds(Math.Min(seconds, MaximumDelay.TotalSeconds));

        NextAttemptAt = now + delay;
    }
}
