using System.Net.WebSockets;
using System.Text.Json;
using InvestmentLab.Identity.Users.Domain;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class HomeAssistantAccessTokenValidator(HomeAssistantCoreOptions core) : IAccessTokenValidator
{
    private const int CurrentUserRequestId = 1;
    private const int ReceiveBufferSize = 4096;

    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(10);

    public async Task<ExternalSignIn?> ValidateAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AnswerTimeout);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(core.WebSocketUri, timeout.Token);

        var userId = await AskCurrentUserAsync(socket, accessToken, timeout.Token);
        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);

        if (userId is null)
            return null;

        return new ExternalSignIn(HomeAssistantAuthenticationHandler.LoginProvider, userId);
    }

    private static async Task<string?> AskCurrentUserAsync(ClientWebSocket socket, string accessToken, CancellationToken cancellationToken)
    {
        await ReceiveAsync(socket, cancellationToken);

        await SendAsync(socket, new { type = "auth", access_token = accessToken }, cancellationToken);
        var authentication = await ReceiveAsync(socket, cancellationToken);
        if (TypeOf(authentication) != "auth_ok")
            return null;

        await SendAsync(socket, new { id = CurrentUserRequestId, type = "auth/current_user" }, cancellationToken);
        var answer = await ReceiveAsync(socket, cancellationToken);

        return UserIdOf(answer);
    }

    private static async Task SendAsync(ClientWebSocket socket, object message, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);

        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    private static async Task<JsonElement> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        using var message = new MemoryStream();
        var buffer = new byte[ReceiveBufferSize];
        ValueWebSocketReceiveResult received;

        do
        {
            received = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
            message.Write(buffer, 0, received.Count);
        }
        while (!received.EndOfMessage);

        return JsonDocument.Parse(message.ToArray()).RootElement.Clone();
    }

    private static string? TypeOf(JsonElement message) =>
        message.TryGetProperty("type", out var type) ? type.GetString() : null;

    private static string? UserIdOf(JsonElement answer)
    {
        var succeeded = answer.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
        if (!succeeded || !answer.TryGetProperty("result", out var result))
            return null;

        return result.TryGetProperty("id", out var id) ? id.GetString() : null;
    }
}
