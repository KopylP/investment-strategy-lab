using System.Linq.Expressions;

namespace InvestmentLab.BuildingBlocks.Messaging;

public sealed class OutboxMessage
{
    public const int MaxAttempts = 24;

    private OutboxMessage() { }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = "";

    public string Payload { get; private set; } = "";

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public string? Error { get; private set; }

    public bool IsAbandoned => ProcessedAt is null && Attempts >= MaxAttempts;

    public static OutboxMessage For(string type, string payload, DateTimeOffset occurredAt) =>
        new() { Id = Guid.CreateVersion7(), Type = type, Payload = payload, OccurredAt = occurredAt.ToUniversalTime() };

    public static Expression<Func<OutboxMessage, bool>> DueAt(DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();

        return message => message.ProcessedAt == null
            && message.Attempts < MaxAttempts
            && (message.NextAttemptAt == null || message.NextAttemptAt <= utcNow);
    }

    public void MarkProcessed(DateTimeOffset processedAt) => ProcessedAt = processedAt.ToUniversalTime();

    public void RecordFailure(string error, DateTimeOffset failedAt)
    {
        Attempts++;
        Error = error;
        NextAttemptAt = failedAt.ToUniversalTime() + OutboxRetryDelay.After(Attempts);
    }
}
