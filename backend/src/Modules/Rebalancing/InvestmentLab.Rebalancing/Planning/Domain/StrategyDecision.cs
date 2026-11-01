using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyDecision(StrategyInfo Strategy, StrategyTargets Targets, SectionDecision Section, Guid? CashInstrumentId)
{
    public IEnumerable<Guid> WantedInstruments
    {
        get
        {
            var targets = Targets.Targets.Select(target => target.InstrumentId);

            return CashInstrumentId is { } cash ? targets.Append(cash) : targets;
        }
    }
}
