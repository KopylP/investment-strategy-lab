using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed record LoadedOperations(
    RebalancingReport Report, IReadOnlyList<JournalOperation> Operations, IReadOnlyDictionary<Guid, string> Sources);
