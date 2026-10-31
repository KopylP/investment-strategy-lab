namespace InvestmentLab.Strategies.Catalog.Domain;

public sealed record ReportDefinition(string Code, string Name, string Description, string Mode, ReportSchedule? Schedule);
