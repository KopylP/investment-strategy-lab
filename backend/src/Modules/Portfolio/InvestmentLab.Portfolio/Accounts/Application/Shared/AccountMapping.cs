using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.Shared;

internal static class AccountMapping
{
    extension(InvestmentAccount account)
    {
        public AccountSetupDto ToSetupDto() => new(
            account.Id.Value,
            account.OwnerId.Value,
            account.Name,
            account.Broker,
            account.BaseCurrency.Code,
            account.TaxPolicyCode,
            account.ContributionPolicy.Kind.ToString(),
            account.ContributionPolicy.DriftBand.Value,
            [.. account.SubPortfolios.Select(sub => new SubPortfolioSetupDto(
                sub.Id.Value,
                sub.Name,
                sub.TargetShare.Value.Value,
                sub.Status.ToString(),
                sub.Strategy?.StrategyId,
                sub.Strategy?.PinnedVersion,
                sub.CashInstrumentId,
                sub.Guardrails.MaxPositionWeight?.Value,
                sub.Guardrails.MaxMonthlyTurnover?.Value))]);
    }
}
