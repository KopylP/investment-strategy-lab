using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application;

/// <summary>A sub-portfolio as the user types it: shares and limits in percent.</summary>
public sealed record SubPortfolioInput(
    string Name,
    decimal TargetSharePercent,
    Guid? StrategyId = null,
    int? PinnedStrategyVersion = null,
    Guid? CashInstrumentId = null,
    decimal? MaxPositionWeightPercent = null,
    decimal? MaxMonthlyTurnoverPercent = null);

public sealed record ContributionPolicyInput(string Kind, decimal? DriftBandPercent);

internal static class AccountInput
{
    public static Result<SubPortfolioDraft> ToDraft(SubPortfolioInput input)
    {
        var share = TargetShare.TryFromPercent(input.TargetSharePercent);
        if (share.IsFailure)
            return share.Error;

        StrategyRef? strategy = null;
        if (input.StrategyId is { } strategyId)
        {
            var reference = StrategyRef.TryOf(strategyId, input.PinnedStrategyVersion);
            if (reference.IsFailure)
                return reference.Error;
            strategy = reference.Value;
        }

        var maxWeight = OptionalPercent(input.MaxPositionWeightPercent);
        if (maxWeight.IsFailure)
            return maxWeight.Error;
        var maxTurnover = OptionalPercent(input.MaxMonthlyTurnoverPercent);
        if (maxTurnover.IsFailure)
            return maxTurnover.Error;

        return new SubPortfolioDraft(input.Name ?? "", share.Value, strategy, input.CashInstrumentId, new Guardrails(maxWeight.Value, maxTurnover.Value));
    }

    public static Result<IReadOnlyDictionary<SubPortfolioId, TargetShare>> ToShares(IReadOnlyDictionary<Guid, decimal>? percents)
    {
        var shares = new Dictionary<SubPortfolioId, TargetShare>();
        foreach (var (id, percent) in percents ?? new Dictionary<Guid, decimal>())
        {
            if (id == Guid.Empty)
                return AccountErrors.TargetSharesIncomplete();
            var share = TargetShare.TryFromPercent(percent);
            if (share.IsFailure)
                return share.Error;
            shares[SubPortfolioId.Of(id)] = share.Value;
        }

        return shares;
    }

    public static Result<ContributionPolicy> ToPolicy(ContributionPolicyInput? input)
    {
        if (input is null)
            return ContributionPolicy.Default;
        if (!Enum.TryParse<ContributionPolicyKind>(input.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return Error.Validation("account.contribution_policy_unknown", $"'{input.Kind}' is not FillUnderweight or FixedSplit.");

        var band = OptionalPercent(input.DriftBandPercent);
        return band.IsFailure ? band.Error : new ContributionPolicy(kind, band.Value ?? ContributionPolicy.DefaultDriftBand);
    }

    private static Result<Percentage?> OptionalPercent(decimal? percent)
    {
        if (percent is not { } value)
            return Result<Percentage?>.Success(null);
        var fraction = Percentage.TryOf(value / 100m);
        return fraction.IsFailure ? fraction.Error : fraction.Value;
    }
}

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

        public TargetSharesChangedV1 ToSharesChanged(DateTimeOffset changedAt) => new(
            account.Id.Value,
            account.ActiveSubPortfolios.ToDictionary(sub => sub.Id.Value, sub => sub.TargetShare.Value.Value),
            changedAt);
    }
}
