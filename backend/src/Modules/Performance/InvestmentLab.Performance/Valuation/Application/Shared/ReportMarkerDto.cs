namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed record ReportMarkerDto(Guid ReportId, DateOnly Date, string Kind, string Period, Guid AccountId);
