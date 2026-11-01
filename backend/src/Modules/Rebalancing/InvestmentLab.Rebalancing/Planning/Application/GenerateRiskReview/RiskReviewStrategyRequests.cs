using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed class RiskReviewStrategyRequests(IPlanningAccounts accounts, IAccountHoldings accountHoldings, TimeProvider clock)
{
    private const decimal NoNewMoney = 0m;

    public async Task<Result<IReadOnlyDictionary<Guid, StrategyAsk>>> BuildAsync(
        GenerateRiskReviewCommand command, CancellationToken cancellationToken)
    {
        var asOf = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        if (await accounts.FindAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var holdings = await accountHoldings.OfAsync(account.Id, account.BaseCurrency, cancellationToken);
        var snapshots = holdings.ToDictionary(sub => sub.Id);

        var asks = planned
            .Where(sub => snapshots.GetValueOrDefault(sub.Id) is { Positions.Count: > 0 })
            .ToDictionary(sub => sub.Id, sub => StrategyAsk.Of(account, sub, snapshots[sub.Id], NoNewMoney, asOf));

        return asks;
    }

    public static List<IPlanningSubPortfolio> PlannedOf(IPlanningAccount account, GenerateRiskReviewCommand command) =>
        [.. PlannedSubPortfolios.Of(account).Where(sub => command.Scope.SubPortfolioIds.Contains(sub.Id))];
}
