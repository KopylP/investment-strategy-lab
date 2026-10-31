using InvestmentLab.MarketData.Contracts.FxRates;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class CrossRates(IFxRates fxRates)
{
    private const string Pln = "PLN";

    public async Task<decimal?> RateAsync(string from, string to, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var fromInPln = await RateInPlnAsync(from, asOf, cancellationToken);
        var toInPln = await RateInPlnAsync(to, asOf, cancellationToken);

        return toInPln > 0m ? fromInPln / toInPln : null;
    }

    private async Task<decimal?> RateInPlnAsync(string currency, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (currency == Pln)
            return 1m;

        var rate = await fxRates.GetFxRate(currency, Pln, asOf.AddDays(1), cancellationToken);

        return rate?.Rate;
    }
}
