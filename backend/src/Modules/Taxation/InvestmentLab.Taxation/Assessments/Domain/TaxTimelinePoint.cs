namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxTimelinePoint(DateOnly Date, decimal Gain, decimal DividendIncome, decimal TaxDue);
