using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record RecordingContext(InvestmentAccount Account, IReadOnlyList<Transaction> Journal, AccountHoldings Holdings, DateTimeOffset Now);
