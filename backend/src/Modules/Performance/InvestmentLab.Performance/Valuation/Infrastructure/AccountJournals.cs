using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class AccountJournals(IJournalQueries journals) : IAccountJournals
{
    public async Task<AccountJournal?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var journal = await journals.GetJournal(accountId, cancellationToken);
        if (journal is null)
            return null;

        List<IJournalEntry> entries = [.. journal.Entries.Select(entry => new JournalEntry(entry))];

        return new AccountJournal(UserId.Of(journal.OwnerId), journal.Currency, entries, journal.ReversedIds);
    }

    private sealed class JournalEntry(TransactionRecordedV1 dto) : IJournalEntry
    {
        private readonly TransactionRecordedV1 _dto = dto;

        public Guid TransactionId => _dto.TransactionId;
        public DateOnly OccurredOn => _dto.OccurredOn;
        public IJournalTrade? Trade { get; } = dto.Trade is { } trade ? new JournalTrade(trade) : null;
        public IJournalCash? Cash { get; } = dto.Cash is { } cash ? new JournalCash(cash) : null;
        public IJournalTransfer? Transfer { get; } = dto.Transfer is { } transfer ? new JournalTransfer(transfer) : null;
    }

    private sealed class JournalTrade(TradeRecordedDto dto) : IJournalTrade
    {
        private readonly TradeRecordedDto _dto = dto;

        public Guid InstrumentId => _dto.InstrumentId;
        public string Side => _dto.Side;
        public decimal Price => _dto.Price;
        public decimal Fee => _dto.Fee;
        public decimal? SettledAmount => _dto.SettledAmount;
        public string? SettledCurrency => _dto.SettledCurrency;
        public IReadOnlyList<IJournalQuantity> Allocations { get; } = [.. dto.Allocations.Select(allocation => new JournalQuantity(allocation))];
    }

    private sealed class JournalQuantity(QuantityAllocationDto dto) : IJournalQuantity
    {
        private readonly QuantityAllocationDto _dto = dto;

        public Guid SubPortfolioId => _dto.SubPortfolioId;
        public decimal Quantity => _dto.Quantity;
    }

    private sealed class JournalCash(CashMovementRecordedDto dto) : IJournalCash
    {
        private readonly CashMovementRecordedDto _dto = dto;

        public string Kind => _dto.Kind;
        public decimal? SettledAmount => _dto.SettledAmount;
        public string? SettledCurrency => _dto.SettledCurrency;
        public IReadOnlyList<IJournalAmount> Allocations { get; } = [.. dto.Allocations.Select(allocation => new JournalAmount(allocation))];
    }

    private sealed class JournalAmount(AmountAllocationDto dto) : IJournalAmount
    {
        private readonly AmountAllocationDto _dto = dto;

        public Guid SubPortfolioId => _dto.SubPortfolioId;
        public decimal Amount => _dto.Amount;
    }

    private sealed class JournalTransfer(InternalTransferRecordedDto dto) : IJournalTransfer
    {
        private readonly InternalTransferRecordedDto _dto = dto;

        public Guid FromSubPortfolioId => _dto.FromSubPortfolioId;
        public Guid ToSubPortfolioId => _dto.ToSubPortfolioId;
        public Guid? InstrumentId => _dto.InstrumentId;
        public decimal? Quantity => _dto.Quantity;
        public decimal? Amount => _dto.Amount;
    }
}
