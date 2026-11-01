namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class NbpPreviousBusinessDay : IFxRule
{
    public string TaxCurrency => "PLN";
    public string SourceName => "NBP";
    public string Description => Message.Of("tax.fx_rule.nbp_previous_business_day");

    public DateOnly LookupDate(DateOnly tradeDate) => tradeDate;
}
