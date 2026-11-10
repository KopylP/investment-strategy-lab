using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record SubPortfolioEvidence(
    InvestmentAccount Account,
    AccountHoldings Holdings,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Universes);
