namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxJournalTrade
{
    Guid InstrumentId { get; }
    string Side { get; }
    decimal Quantity { get; }
    decimal Price { get; }
    decimal Fee { get; }
    decimal? SettledAmount { get; }
    string? SettledCurrency { get; }
    IReadOnlyList<EntryAllocation> Allocations { get; }
}
