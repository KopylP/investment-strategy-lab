namespace InvestmentLab.Taxation.Assessments.Domain;

public interface IFxRule
{
    string TaxCurrency { get; }
    string SourceName { get; }
    string Description { get; }

    DateOnly LookupDate(DateOnly tradeDate);
}
