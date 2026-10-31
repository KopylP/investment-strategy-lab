namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TradeDetails(Guid InstrumentId, TradeSide Side, Quantity Quantity, Price Price, Money Fee, IReadOnlyList<Allocation> Allocations, Money? Settlement = null)
    : TransactionDetails;
