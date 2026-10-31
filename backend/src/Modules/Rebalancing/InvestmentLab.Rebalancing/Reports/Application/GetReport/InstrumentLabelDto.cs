namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record InstrumentLabelDto(string Symbol, string Name, string? Isin, string Currency);
