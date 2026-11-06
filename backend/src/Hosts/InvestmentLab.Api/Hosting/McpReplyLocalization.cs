using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.Strategies.Contracts;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace InvestmentLab.Api.Hosting;

public static class McpReplyLocalization
{
    public static IMcpServerBuilder WithLocalizedReplies(this IMcpServerBuilder server) =>
        server.WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            var result = await next(context, cancellationToken);
            var localizer = await LocalizerAsync(context.Services, cancellationToken);

            return Localized(result, localizer);
        }));

    private static CallToolResult Localized(CallToolResult result, MessageLocalizer localizer)
    {
        foreach (var text in result.Content.OfType<TextContentBlock>())
            text.Text = McpReply.Rendered(text.Text, localizer);

        return result;
    }

    private static async Task<MessageLocalizer> LocalizerAsync(IServiceProvider? services, CancellationToken cancellationToken)
    {
        if (services?.GetService<IStrategyDirectory>() is not { } strategies)
            return MessageLocalizer.Default;

        try
        {
            return new MessageLocalizer(await strategies.GetMessages(cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MessageLocalizer.Default;
        }
    }
}
