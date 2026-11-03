namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public static class FundErrors
{
    public static Error PlaceRequired() => Error.Validation("builder.funds.place_required", "A search needs an exchange or a region.");

    public static Error TooManyWords() => Error.Validation("builder.funds.too_many_words", "At most five name words are allowed.");

    public static Error InvalidExpenseRatio() => Error.Validation("builder.funds.expense_ratio", "The yearly fee must be between zero and 0.1.");

    public static Error InvalidAssets() => Error.Validation("builder.funds.assets", "Minimum assets cannot be negative.");

    public static Error RegistrationFailed() => Error.Conflict("builder.funds.registration_failed", "The fund could not be registered.");

    public static Error TooManyFunds(int max) => Error.Validation("builder.funds.too_many_funds", "The selection exceeds the draft fund limit.", new { max });

    public static Error InvalidFund() => Error.Validation("builder.funds.invalid", "A fund needs a symbol, name and trading currency.");

    public static Error SearchUnavailable() => Error.Conflict("market_data.fund_screen.unavailable", "Fund search is unavailable.");
}
