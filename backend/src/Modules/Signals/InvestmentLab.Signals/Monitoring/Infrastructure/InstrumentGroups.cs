using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class InstrumentGroups(IInstrumentCatalog instruments) : IInstrumentGroups
{
    public async Task<IReadOnlyList<GroupMember>> MembersOfAsync(string groupName, CancellationToken cancellationToken)
    {
        var catalog = await instruments.GetInstruments(cancellationToken);
        var members = catalog.Where(instrument => instrument.Group == groupName);

        return [.. members.Select(instrument => new GroupMember(instrument.Id, instrument.Label))];
    }
}
