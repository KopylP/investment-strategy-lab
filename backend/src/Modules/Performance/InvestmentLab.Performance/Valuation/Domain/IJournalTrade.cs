namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalTrade
{
    Guid InstrumentId { get; }
    string Side { get; }
    decimal Price { get; }
    decimal Fee { get; }
    decimal? SettledAmount { get; }
    string? SettledCurrency { get; }
    IReadOnlyList<IJournalQuantity> Allocations { get; }
}
