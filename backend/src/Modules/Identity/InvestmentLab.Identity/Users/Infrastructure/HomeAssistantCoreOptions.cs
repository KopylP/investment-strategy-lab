namespace InvestmentLab.Identity.Users.Infrastructure;

public sealed record HomeAssistantCoreOptions(Uri Address)
{
    public const string Section = "HomeAssistant:Core";
    public const string DefaultAddress = "http://homeassistant:8123";

    private const string WebSocketPath = "api/websocket";

    public Uri WebSocketUri
    {
        get
        {
            var webSocket = new UriBuilder(new Uri(Address, WebSocketPath))
            {
                Scheme = Address.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
            };

            return webSocket.Uri;
        }
    }
}
