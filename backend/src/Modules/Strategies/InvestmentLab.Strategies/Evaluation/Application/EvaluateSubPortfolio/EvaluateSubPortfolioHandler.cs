using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;

namespace InvestmentLab.Strategies.Evaluation.Application.EvaluateSubPortfolio;

/// <param name="AsOf">Null: today.</param>
public sealed record EvaluateSubPortfolioCommand(Guid AccountId, Guid SubPortfolioId, DateOnly? AsOf = null, decimal Contribution = 0m, int? Version = null);

/// <summary>Runs a strategy on what a real sub-portfolio holds: a preview of the monthly decision or of the weekly risk check.</summary>
public sealed class EvaluateSubPortfolioHandler(
    IAccountSetupQueries accounts, IHoldingsQueries holdings, EvaluateStrategyHandler evaluate, TimeProvider clock)
{
    public async Task<Result<StrategyDecisionDto>> Decide(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var request = await RequestAsync(strategyId, command, cancellationToken);
        return request.IsFailure ? request.Error : await evaluate.Decide(request.Value, cancellationToken);
    }

    public async Task<Result<RiskAssessmentDto>> AssessRisk(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var request = await RequestAsync(strategyId, command, cancellationToken);
        return request.IsFailure ? request.Error : await evaluate.AssessRisk(request.Value, cancellationToken);
    }

    private async Task<Result<EvaluateStrategyRequest>> RequestAsync(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var asOf = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (await accounts.GetAccountSetup(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");
        if (account.SubPortfolios.FirstOrDefault(sub => sub.Id == command.SubPortfolioId) is not { } sub)
            return Error.NotFound("account.sub_portfolio_not_found", $"Sub-portfolio {command.SubPortfolioId} is not part of the account.");

        var snapshot = await holdings.GetSubPortfolioSnapshot(command.AccountId, command.SubPortfolioId, asOf, cancellationToken);
        var state = new SubPortfolioStateDto(
            account.BaseCurrency,
            Math.Max(0m, snapshot?.Cash ?? 0m),
            [.. (snapshot?.Positions ?? []).Select(position => new HoldingStateDto(
                position.InstrumentId,
                [.. position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.OpenedOn))]))]);

        return new EvaluateStrategyRequest(strategyId, command.Version ?? sub.PinnedStrategyVersion, state, command.Contribution, asOf, sub.CashInstrumentId);
    }
}
