namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record InstrumentExposure(AssetClass AssetClass, InvestmentRegion Region, Currency? AssetCurrency)
{
    public static Result<InstrumentExposure?> TryOf(string? assetClass, string? region, string? assetCurrency)
    {
        if (assetClass is null && region is null && assetCurrency is null)
            return Result<InstrumentExposure?>.Success(null);

        if (assetClass is null || region is null)
            return InstrumentErrors.ExposureIncomplete();

        var knownClass = EnumText.TryParse<AssetClass>(assetClass, "instrument.asset_class_unknown");
        if (knownClass.IsFailure)
            return knownClass.Error;

        var knownRegion = EnumText.TryParse<InvestmentRegion>(region, "instrument.region_unknown");
        if (knownRegion.IsFailure)
            return knownRegion.Error;

        if (assetCurrency is null)
            return new InstrumentExposure(knownClass.Value, knownRegion.Value, null);

        var currency = Currency.TryOf(assetCurrency);
        if (currency.IsFailure)
            return currency.Error;

        return new InstrumentExposure(knownClass.Value, knownRegion.Value, currency.Value);
    }
}
