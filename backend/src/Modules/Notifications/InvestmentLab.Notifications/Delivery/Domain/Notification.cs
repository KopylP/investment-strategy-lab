namespace InvestmentLab.Notifications.Delivery.Domain;

public sealed record Notification(
    Guid RecipientId,
    string Topic,
    string Tag,
    string Title,
    string Message,
    IReadOnlyDictionary<string, object?> Data);
