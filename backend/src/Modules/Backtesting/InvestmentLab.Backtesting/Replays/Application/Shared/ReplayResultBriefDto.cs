using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayResultBriefDto(
    DateOnly Start,
    DateOnly End,
    string Currency,
    decimal InitialCapital,
    decimal FinalCapital,
    int Trades,
    int RiskExits,
    int SignalReports,
    IReadOnlyList<ReplaySeriesBriefDto> Series)
{
    public static ReplayResultBriefDto From(ReplayResultBrief replayResultBrief) => new(
        Start: replayResultBrief.Start,
        End: replayResultBrief.End,
        Currency: replayResultBrief.Currency,
        InitialCapital: replayResultBrief.InitialCapital,
        FinalCapital: replayResultBrief.FinalCapital,
        Trades: replayResultBrief.Trades,
        RiskExits: replayResultBrief.RiskExits,
        SignalReports: replayResultBrief.SignalReports,
        Series: [.. replayResultBrief.Series.Select(ReplaySeriesBriefDto.From)]);
}
