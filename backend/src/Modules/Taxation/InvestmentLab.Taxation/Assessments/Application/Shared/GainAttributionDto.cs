using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record GainAttributionDto(
    Guid SubPortfolioId,
    decimal GainInTaxCurrency)
{
    public static GainAttributionDto From(GainAttribution gainAttribution) => new(
        SubPortfolioId: gainAttribution.SubPortfolioId,
        GainInTaxCurrency: gainAttribution.GainInTaxCurrency);
}
