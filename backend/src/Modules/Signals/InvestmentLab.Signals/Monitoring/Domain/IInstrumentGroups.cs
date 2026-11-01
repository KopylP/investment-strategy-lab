namespace InvestmentLab.Signals.Monitoring.Domain;

public interface IInstrumentGroups
{
    Task<IReadOnlyList<GroupMember>> MembersOfAsync(string groupName, CancellationToken cancellationToken);
}
