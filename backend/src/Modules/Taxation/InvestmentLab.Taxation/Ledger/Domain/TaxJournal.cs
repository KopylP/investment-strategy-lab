namespace InvestmentLab.Taxation.Ledger.Domain;

public sealed record TaxJournal(IReadOnlyList<ITaxJournalEntry> Entries, IReadOnlyList<Guid> ReversedIds);
