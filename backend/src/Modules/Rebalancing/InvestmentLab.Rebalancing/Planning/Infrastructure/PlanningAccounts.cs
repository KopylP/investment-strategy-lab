using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class PlanningAccounts(IAccountSetupQueries accounts) : IPlanningAccounts
{
    private const string ActiveStatus = "Active";

    public async Task<IPlanningAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.GetAccountSetup(accountId, cancellationToken);

        return setup is null ? null : new Account(setup);
    }

    private sealed class Account(AccountSetupDto dto) : IPlanningAccount
    {
        private readonly AccountSetupDto _dto = dto;

        public Guid Id => _dto.Id;
        public UserId OwnerId => UserId.Of(_dto.OwnerId);
        public string Name => _dto.Name;
        public string BaseCurrency => _dto.BaseCurrency;
        public string ContributionPolicy => _dto.ContributionPolicy;
        public decimal DriftBand => _dto.DriftBand;
        public IReadOnlyList<IPlanningSubPortfolio> SubPortfolios { get; } = [.. dto.SubPortfolios.Select(sub => new SubPortfolio(sub))];
    }

    private sealed class SubPortfolio(SubPortfolioSetupDto dto) : IPlanningSubPortfolio
    {
        private readonly SubPortfolioSetupDto _dto = dto;

        public Guid Id => _dto.Id;
        public string Name => _dto.Name;
        public decimal TargetShare => _dto.TargetShare;
        public bool IsActive => _dto.Status == ActiveStatus;
        public Guid? StrategyId => _dto.StrategyId;
        public int? PinnedStrategyVersion => _dto.PinnedStrategyVersion;
        public Guid? CashInstrumentId => _dto.CashInstrumentId;
        public decimal? MaxPositionWeight => _dto.MaxPositionWeight;
        public decimal? MaxMonthlyTurnover => _dto.MaxMonthlyTurnover;
    }
}
