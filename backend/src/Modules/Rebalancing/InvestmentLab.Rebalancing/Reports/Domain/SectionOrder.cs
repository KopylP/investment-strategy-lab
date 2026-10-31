namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionOrder(string Kind, Guid InstrumentId, decimal Quantity, decimal Price, decimal Amount, string Reason);
