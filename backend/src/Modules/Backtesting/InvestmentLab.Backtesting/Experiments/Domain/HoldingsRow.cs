using System.Text.Json.Serialization;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record HoldingsRow(DateOnly Date, bool? MarketFavorable, IReadOnlyList<HeldShare> Positions, double CashShare)
{
    [JsonIgnore]
    public double InvestedShare => 1d - CashShare;

    public bool IsWithin(DateOnly? from, DateOnly? to) => (from is null || Date >= from) && (to is null || Date <= to);
}
