namespace InvestmentLab.Portfolio.Accounts.Domain;

public static class AccountErrors
{
    public static Error NotFound(Guid accountId) => Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

    public static Error NameRequired() => Error.Validation("account.name_required", "Name must not be blank.");

    public static Error BrokerRequired() => Error.Validation("account.broker_required", "Broker must not be blank.");

    public static Error UnsupportedBaseCurrency(Currency currency) =>
        Error.Validation("account.base_currency_unsupported", $"An account is kept in EUR or USD, not {currency}.");

    public static Error NoSubPortfolios() => Error.Validation("account.no_sub_portfolios", "An account needs at least one sub-portfolio.");

    public static Error TargetShareOutOfRange(decimal percent) =>
        Error.Validation("account.target_share_out_of_range", $"Target share {percent}% must be above 0% and at most 100%.");

    public static Error TargetSharesDoNotAddUp(decimal totalPercent) =>
        Error.Validation("account.target_shares_not_100", $"Target shares of active sub-portfolios add up to {totalPercent:0.##}%, not 100%.");

    public static Error TargetSharesIncomplete() =>
        Error.Validation("account.target_shares_incomplete", "Give a target share for every active sub-portfolio and no other.");

    public static Error SubPortfolioNotFound(Guid subPortfolioId) =>
        Error.NotFound("account.sub_portfolio_not_found", $"Sub-portfolio {subPortfolioId} is not an active part of the account.");

    public static Error SubPortfolioNameTaken(string name) =>
        Error.Conflict("account.sub_portfolio_name_taken", $"An active sub-portfolio is already called '{name}'.");

    public static Error LastSubPortfolio() =>
        Error.Validation("account.last_sub_portfolio", "The last active sub-portfolio cannot be retired.");

    public static Error StrategyRequired() => Error.Validation("account.strategy_required", "Strategy id must not be empty.");

    public static Error StrategyVersionInvalid(int version) =>
        Error.Validation("account.strategy_version_invalid", $"Strategy version {version} must be 1 or more.");
}
