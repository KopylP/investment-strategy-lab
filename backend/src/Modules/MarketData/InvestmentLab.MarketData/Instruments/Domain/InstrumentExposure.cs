namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record InstrumentExposure(AssetClass AssetClass, InvestmentRegion Region, Currency? AssetCurrency)
{
    public static Result<InstrumentExposure?> TryOf(AssetClass? assetClass, InvestmentRegion? region, string? assetCurrency)
    {
        if (assetClass is null && region is null && assetCurrency is null)
            return Result<InstrumentExposure?>.Success(null);

        if (assetClass is not { } knownClass || region is not { } knownRegion)
            return InstrumentErrors.ExposureIncomplete();

        if (assetCurrency is null)
            return new InstrumentExposure(knownClass, knownRegion, null);

        var currency = Currency.TryOf(assetCurrency);
        if (currency.IsFailure)
            return currency.Error;

        return new InstrumentExposure(knownClass, knownRegion, currency.Value);
    }
}
