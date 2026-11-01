using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed class RiskReviewStrategyRequests(PlanningInputs inputs, TimeProvider clock)
{
    public async Task<Result<IReadOnlyDictionary<Guid, EvaluateStrategyRequest>>> BuildAsync(
        GenerateRiskReviewCommand command, CancellationToken cancellationToken)
    {
        var asOf = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var holdings = await inputs.HoldingsAsync(account, cancellationToken);
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);

        return planned
            .Where(sub => snapshots.GetValueOrDefault(sub.Id) is { Positions.Count: > 0 })
            .ToDictionary(sub => sub.Id, sub => new EvaluateStrategyRequest(
                sub.StrategyId!.Value,
                sub.PinnedStrategyVersion,
                PlanningInputs.StateOf(account, snapshots[sub.Id]),
                0m,
                asOf,
                sub.CashInstrumentId));
    }

    public static List<SubPortfolioSetupDto> PlannedOf(AccountSetupDto account, GenerateRiskReviewCommand command) =>
        [.. PlanningInputs.Planned(account).Where(sub => command.Scope.SubPortfolioIds.Contains(sub.Id))];
}
