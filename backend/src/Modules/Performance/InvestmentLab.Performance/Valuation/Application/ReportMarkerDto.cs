namespace InvestmentLab.Performance.Valuation.Application;

public sealed record ReportMarkerDto(Guid ReportId, DateOnly Date, string Kind, string Period, Guid AccountId);
