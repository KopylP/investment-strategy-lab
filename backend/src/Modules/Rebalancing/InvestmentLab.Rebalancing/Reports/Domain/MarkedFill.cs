namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record MarkedFill(ReportAction Action, Fill Fill, FillReference Reference);
