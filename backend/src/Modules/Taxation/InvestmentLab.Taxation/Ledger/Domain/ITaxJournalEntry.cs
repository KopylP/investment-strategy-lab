namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxJournalEntry
{
    Guid TransactionId { get; }
    Guid AccountId { get; }
    UserId OwnerId { get; }
    DateOnly OccurredOn { get; }
    string Currency { get; }
    ITaxJournalTrade? Trade { get; }
    ITaxJournalCash? Cash { get; }
}
