namespace InvestmentLab.Performance.Valuation.Domain;

public sealed record Movement(
    DateOnly Date,
    int Sequence,
    Guid SubPortfolioId,
    Guid? InstrumentId,
    decimal QuantityDelta,
    decimal CashDelta,
    decimal ExternalFlow,
    decimal TransferQuantity = 0m,
    decimal? TradePrice = null,
    string? CashCurrency = null,
    decimal? UnitCost = null);

public delegate decimal? PriceOn(Guid instrumentId, DateOnly day);

public delegate decimal? FxOn(string currency, DateOnly day);
