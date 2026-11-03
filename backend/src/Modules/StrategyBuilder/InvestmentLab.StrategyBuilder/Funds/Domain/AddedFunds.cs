namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public sealed record AddedFunds(IReadOnlyList<Guid> InstrumentIds, IReadOnlyList<FundNotAdded> Failed)
{
    public AddedFunds With(string symbol, Guid? instrumentId, string? reason)
    {
        if (instrumentId is { } id)
        {
            var ids = InstrumentIds.Append(id).Distinct().ToList();

            return this with { InstrumentIds = ids };
        }

        var failure = new FundNotAdded(symbol, reason ?? FundErrors.RegistrationFailed().AsMessage());

        return this with { Failed = [.. Failed, failure] };
    }
}
