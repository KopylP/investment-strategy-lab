namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayResultBrief(
    DateOnly Start,
    DateOnly End,
    string Currency,
    decimal InitialCapital,
    decimal FinalCapital,
    int Trades,
    int RiskExits,
    int SignalReports,
    IReadOnlyList<ReplaySeriesBrief> Series)
{
    public static ReplayResultBrief? Of(ReplayResult? result, bool includePoints)
    {
        if (result is null)
            return null;

        var series = result.Series.Select(line => ReplaySeriesBrief.Of(line, result.Start, includePoints)).ToList();

        return new ReplayResultBrief(
            Start: result.Start,
            End: result.End,
            Currency: result.Currency,
            InitialCapital: result.InitialCapital,
            FinalCapital: result.FinalCapital,
            Trades: result.Trades,
            RiskExits: result.RiskExits,
            SignalReports: result.SignalReports,
            Series: series);
    }
}
