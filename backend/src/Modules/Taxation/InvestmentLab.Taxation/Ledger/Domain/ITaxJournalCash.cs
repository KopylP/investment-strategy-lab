namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxJournalCash
{
    string Kind { get; }
    decimal Amount { get; }
    Guid? InstrumentId { get; }
    decimal? SettledAmount { get; }
    string? SettledCurrency { get; }
    IReadOnlyList<EntryAllocation> Allocations { get; }
}
