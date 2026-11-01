using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Ledger.Infrastructure;

internal sealed class TaxJournals(IJournalQueries journals) : ITaxJournals
{
    public async Task<TaxJournal?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var journal = await journals.GetJournal(accountId, cancellationToken);
        if (journal is null)
            return null;

        List<ITaxJournalEntry> entries = [.. journal.Entries.Select(entry => new JournalEntry(entry))];

        return new TaxJournal(entries, journal.ReversedIds);
    }

    private sealed class JournalEntry(TransactionRecordedV1 dto) : ITaxJournalEntry
    {
        private readonly TransactionRecordedV1 _dto = dto;

        public Guid TransactionId => _dto.TransactionId;
        public Guid AccountId => _dto.AccountId;
        public UserId OwnerId => UserId.Of(_dto.OwnerId);
        public DateOnly OccurredOn => _dto.OccurredOn;
        public string Currency => _dto.Currency;
        public ITaxJournalTrade? Trade { get; } = dto.Trade is { } trade ? new JournalTrade(trade) : null;
        public ITaxJournalCash? Cash { get; } = dto.Cash is { } cash ? new JournalCash(cash) : null;
    }

    private sealed class JournalTrade(TradeRecordedDto dto) : ITaxJournalTrade
    {
        private readonly TradeRecordedDto _dto = dto;

        public Guid InstrumentId => _dto.InstrumentId;
        public string Side => _dto.Side;
        public decimal Quantity => _dto.Quantity;
        public decimal Price => _dto.Price;
        public decimal Fee => _dto.Fee;
        public decimal? SettledAmount => _dto.SettledAmount;
        public string? SettledCurrency => _dto.SettledCurrency;
        public IReadOnlyList<EntryAllocation> Allocations { get; } =
            [.. dto.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Quantity))];
    }

    private sealed class JournalCash(CashMovementRecordedDto dto) : ITaxJournalCash
    {
        private readonly CashMovementRecordedDto _dto = dto;

        public string Kind => _dto.Kind;
        public decimal Amount => _dto.Amount;
        public Guid? InstrumentId => _dto.InstrumentId;
        public decimal? SettledAmount => _dto.SettledAmount;
        public string? SettledCurrency => _dto.SettledCurrency;
        public IReadOnlyList<EntryAllocation> Allocations { get; } =
            [.. dto.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Amount))];
    }
}
