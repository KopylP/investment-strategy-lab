namespace InvestmentLab.Notifications.Delivery.Infrastructure;

public sealed record HomeAssistantChannelOptions(string? PanelPath)
{
    public const string Section = "Notifications:HomeAssistant";
}
