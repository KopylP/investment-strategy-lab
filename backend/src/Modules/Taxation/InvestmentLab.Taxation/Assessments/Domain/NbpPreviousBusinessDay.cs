namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class NbpPreviousBusinessDay : IFxRule
{
    public string TaxCurrency => "PLN";
    public string SourceName => "NBP";
    public string Description => "NBP, остання таблиця A перед днем угоди";

    public DateOnly LookupDate(DateOnly tradeDate) => tradeDate;
}
