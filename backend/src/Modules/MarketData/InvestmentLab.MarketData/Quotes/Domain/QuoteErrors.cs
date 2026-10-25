namespace InvestmentLab.MarketData.Quotes.Domain;

public static class QuoteErrors
{
    public static Error PriceNotPositive() => Error.Validation("quote.price_not_positive", "Quote prices must be positive.");

    public static Error ReasonRequired() => Error.Validation("quote.reason_required", "A correction needs a reason.");

    public static Error InstrumentNotFound(Guid id) => Error.NotFound("quote.instrument_not_found", $"Instrument {id} does not exist.");
}
