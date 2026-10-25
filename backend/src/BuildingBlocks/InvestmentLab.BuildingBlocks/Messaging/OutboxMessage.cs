namespace InvestmentLab.BuildingBlocks.Messaging;

/// <summary>An integration event saved in the same transaction as the change that raised it.</summary>
public sealed class OutboxMessage
{
    public const int MaxAttempts = 10;

    private OutboxMessage() { }

    public Guid Id { get; private set; }

    /// <summary>Full CLR name of the event contract; handlers are found by it.</summary>
    public string Type { get; private set; } = "";

    public string Payload { get; private set; } = "";

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage For(string type, string payload, DateTimeOffset occurredAt) =>
        new() { Id = Guid.CreateVersion7(), Type = type, Payload = payload, OccurredAt = occurredAt.ToUniversalTime() };

    // Npgsql writes timestamptz only from UTC offsets.
    public void MarkProcessed(DateTimeOffset processedAt) => ProcessedAt = processedAt.ToUniversalTime();

    public void RecordFailure(string error)
    {
        Attempts++;
        Error = error;
    }
}
