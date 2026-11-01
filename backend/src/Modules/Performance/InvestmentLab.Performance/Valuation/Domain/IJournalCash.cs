namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalCash
{
    string Kind { get; }
    decimal? SettledAmount { get; }
    string? SettledCurrency { get; }
    IReadOnlyList<IJournalAmount> Allocations { get; }
}
