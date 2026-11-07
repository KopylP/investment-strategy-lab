using System.ComponentModel;
using InvestmentLab.MarketData.Quotes.Application.GetQuoteCoverage;
using ModelContextProtocol.Server;

namespace InvestmentLab.MarketData.Quotes.Api;

[McpServerToolType]
internal static class QuoteTools
{
    [McpServerTool(Name = "get_instrument_history", ReadOnly = true)]
    [Description("From when to when each instrument has daily quotes (archive included, as backtests read them), how many, and the longest gap in days. Check it before choosing a backtest period.")]
    public static Task<IReadOnlyList<QuoteCoverageDto>> GetInstrumentHistory(
        [Description("Ids of the instruments, as list_instruments gives them.")] Guid[] instrumentIds,
        GetQuoteCoverageHandler handler,
        CancellationToken cancellationToken) =>
        handler.Handle(instrumentIds, cancellationToken);
}
