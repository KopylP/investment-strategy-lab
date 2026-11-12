using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayResultDto(
    DateOnly Start,
    DateOnly End,
    string Currency,
    int Trades,
    int RiskExits,
    int SignalReports,
    IReadOnlyList<ReplaySeriesDto> Series,
    IReadOnlyList<ReplayJournalEntryDto>? Journal,
    decimal InitialCapital,
    decimal FinalCapital)
{
    public static ReplayResultDto From(ReplayResult replayResult) => new(
        Start: replayResult.Start,
        End: replayResult.End,
        Currency: replayResult.Currency,
        Trades: replayResult.Trades,
        RiskExits: replayResult.RiskExits,
        SignalReports: replayResult.SignalReports,
        Series: [.. replayResult.Series.Select(ReplaySeriesDto.From)],
        Journal: replayResult.Journal?.Select(ReplayJournalEntryDto.From).ToList(),
        InitialCapital: replayResult.InitialCapital,
        FinalCapital: replayResult.FinalCapital);
}
