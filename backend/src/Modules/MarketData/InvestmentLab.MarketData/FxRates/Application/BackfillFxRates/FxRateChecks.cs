namespace InvestmentLab.MarketData.FxRates.Application.BackfillFxRates;

public sealed class FxRateChecks(TimeProvider clock)
{
    private static readonly TimeSpan NewRatesFreshFor = TimeSpan.FromHours(1);

    private readonly Lock _gate = new();
    private DateTimeOffset? _newRatesCheckedAt;
    private DateOnly? _olderRatesCheckedFrom;

    public bool NewRatesCheckedRecently()
    {
        lock (_gate)
            return _newRatesCheckedAt is { } checkedAt && clock.GetUtcNow() - checkedAt < NewRatesFreshFor;
    }

    public bool OlderRatesCheckedFrom(DateOnly from)
    {
        lock (_gate)
            return _olderRatesCheckedFrom is { } checkedFrom && checkedFrom <= from;
    }

    public void RecordNewRates()
    {
        lock (_gate)
            _newRatesCheckedAt = clock.GetUtcNow();
    }

    public void RecordOlderRatesFrom(DateOnly from)
    {
        lock (_gate)
            _olderRatesCheckedFrom = _olderRatesCheckedFrom is { } earlier && earlier < from ? earlier : from;
    }
}
