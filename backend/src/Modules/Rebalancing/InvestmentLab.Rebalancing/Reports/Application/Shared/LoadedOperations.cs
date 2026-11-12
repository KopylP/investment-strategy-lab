using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Shared;

public sealed record LoadedOperations(
    RebalancingReport Report, IReadOnlyList<JournalOperation> Operations, IReadOnlyDictionary<Guid, string> Sources);
