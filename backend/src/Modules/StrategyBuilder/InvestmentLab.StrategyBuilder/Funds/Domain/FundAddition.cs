using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public sealed record FundAddition(IReadOnlyList<FoundFund> Funds, IReadOnlyList<Guid> SelectedInstrumentIds)
{
    public static Result<FundAddition> TryOf(IReadOnlyList<FoundFund> funds, IReadOnlyList<Guid> selectedInstrumentIds)
    {
        var distinctFunds = funds.DistinctBy(fund => fund.Symbol.Trim(), StringComparer.OrdinalIgnoreCase).ToList();
        var knownIds = distinctFunds.Where(fund => fund.InstrumentId.HasValue).Select(fund => fund.InstrumentId!.Value);
        var knownCount = selectedInstrumentIds.Concat(knownIds).Distinct().Count();
        var unknownCount = distinctFunds.Count(fund => fund.InstrumentId is null);

        if (knownCount + unknownCount > DraftSetup.MaxFunds)
            return FundErrors.TooManyFunds(DraftSetup.MaxFunds);

        return new FundAddition(distinctFunds, selectedInstrumentIds);
    }
}
