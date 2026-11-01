namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyAnswer<T>(T? Value, string? ErrorMessage)
    where T : class
{
    public bool IsSuccess => Value is not null;

    public static StrategyAnswer<T> Given(T value) => new(value, null);

    public static StrategyAnswer<T> Failed(string error) => new(null, error);
}
