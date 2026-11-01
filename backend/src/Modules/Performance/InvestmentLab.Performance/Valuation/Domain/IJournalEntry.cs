namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalEntry
{
    Guid TransactionId { get; }
    DateOnly OccurredOn { get; }
    IJournalTrade? Trade { get; }
    IJournalCash? Cash { get; }
    IJournalTransfer? Transfer { get; }
}
