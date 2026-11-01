namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record BuiltInSignal(Guid SignalId, UserId OwnerId, Guid? AccountId, string Kind)
{
    public bool Concerns(DeclaringAccount account) =>
        OwnerId == account.OwnerId && (AccountId is null || AccountId == account.Id);
}
