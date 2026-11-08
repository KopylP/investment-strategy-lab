namespace InvestmentLab.Notifications.Delivery.Domain;

public sealed record Recipient(Guid UserId, string? HomeAssistantUserId, IReadOnlyList<string>? ChosenDestinations, Language? ChosenLanguage = null)
{
    public IEnumerable<Destination> Targets(IEnumerable<Destination> available) =>
        ChosenDestinations is { } chosen
            ? available.Where(destination => chosen.Contains(destination.Id))
            : available.Where(destination => destination.Suggested);
}
