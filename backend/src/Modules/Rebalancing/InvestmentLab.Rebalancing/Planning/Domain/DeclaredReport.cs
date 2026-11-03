using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record DeclaredReport(
    string Code,
    string Name,
    string Description,
    string Mode,
    ReportKind Kind,
    SubPortfolioReport SubPortfolio,
    IReadOnlyList<string> SignalKinds,
    IReadOnlyList<Guid>? SignalIds = null);
