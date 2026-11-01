namespace InvestmentLab.Performance.Valuation.Domain;

public sealed record AccountJournal(UserId OwnerId, string Currency, IReadOnlyList<IJournalEntry> Entries, IReadOnlyList<Guid> ReversedIds);
