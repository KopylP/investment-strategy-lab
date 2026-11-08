using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.SharedKernel;
using ModelContextProtocol;

namespace InvestmentLab.BuildingBlocks.Mcp;

public static class ToolFailures
{
    extension(Result result)
    {
        public void ThrowIfFailed()
        {
            if (result.IsFailure)
                throw Explained(result.Error);
        }
    }

    extension<T>(Result<T> result)
    {
        public T ValueOrThrow() => result.IsSuccess ? result.Value : throw Explained(result.Error);
    }

    private static McpException Explained(Error error)
    {
        var token = error.AsMessage();
        var rendered = MessageLocalizer.In(McpReply.Language).Render(token);
        var explanation = rendered == token ? error.Message : rendered;

        return new McpException($"{explanation} [{error.Code}]");
    }
}
