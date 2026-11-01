namespace InvestmentLab.Signals.Monitoring.Domain;

public static class BuiltInSignalKind
{
    public const string VolatilityIndex = "volatility-index";
    public const string AccountDrawdown = "account-drawdown";
    public const string PositionDrawdown = "position-drawdown";

    private const string VolatilityIndexKeyPrefix = "vix";
    private const char KeySeparator = ':';

    public static string VolatilityIndexKey(Guid ownerId) => KeyOf(VolatilityIndexKeyPrefix, ownerId);

    public static string AccountDrawdownKey(Guid accountId) => KeyOf(AccountDrawdown, accountId);

    public static string PositionDrawdownKey(Guid accountId) => KeyOf(PositionDrawdown, accountId);

    public static string OfKey(string builtInKey)
    {
        var prefix = builtInKey.Split(KeySeparator)[0];

        return prefix switch
        {
            VolatilityIndexKeyPrefix => VolatilityIndex,
            AccountDrawdown => AccountDrawdown,
            PositionDrawdown => PositionDrawdown,
            _ => throw new ArgumentException($"'{builtInKey}' is not the key of a known built-in signal.", nameof(builtInKey)),
        };
    }

    private static string KeyOf(string prefix, Guid id) => $"{prefix}{KeySeparator}{id:N}";
}
