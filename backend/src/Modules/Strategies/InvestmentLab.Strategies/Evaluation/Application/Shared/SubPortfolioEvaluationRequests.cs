using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed class SubPortfolioEvaluationRequests(IAccountSetupQueries accounts, IHoldingsQueries holdings, TimeProvider clock)
{
    public async Task<Result<EvaluateStrategyRequest>> BuildAsync(
        Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var asOf = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        if (await accounts.GetAccountSetup(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        if (account.SubPortfolios.FirstOrDefault(sub => sub.Id == command.SubPortfolioId) is not { } sub)
            return Error.NotFound("account.sub_portfolio_not_found", $"Sub-portfolio {command.SubPortfolioId} is not part of the account.");

        var snapshot = await holdings.GetSubPortfolioSnapshot(command.AccountId, command.SubPortfolioId, asOf, cancellationToken);
        var state = StateOf(account, snapshot);
        var version = command.Version ?? sub.PinnedStrategyVersion;

        return new EvaluateStrategyRequest(strategyId, version, state, command.Contribution, asOf, sub.CashInstrumentId);
    }

    private static SubPortfolioStateDto StateOf(AccountSetupDto account, SubPortfolioHoldingsDto? snapshot)
    {
        var cash = Math.Max(0m, snapshot?.Cash ?? 0m);
        var positions = snapshot?.Positions ?? [];

        return new SubPortfolioStateDto(account.BaseCurrency, cash, [.. positions.Select(HoldingStateOf)]);
    }

    private static HoldingStateDto HoldingStateOf(PositionDto position)
    {
        var lots = position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.OpenedOn));

        return new HoldingStateDto(position.InstrumentId, [.. lots]);
    }
}
