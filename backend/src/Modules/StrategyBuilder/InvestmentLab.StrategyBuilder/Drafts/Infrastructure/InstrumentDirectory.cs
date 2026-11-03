using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class InstrumentDirectory(IInstrumentCatalog catalog) : IInstrumentDirectory
{
    public async Task<IReadOnlyList<DirectoryInstrument>> AllAsync(CancellationToken cancellationToken)
    {
        var instruments = await catalog.GetInstruments(cancellationToken);

        return [.. instruments.Select(ToDirectory)];
    }

    private static DirectoryInstrument ToDirectory(InstrumentDto instrument) =>
        new(instrument.Id, instrument.Label, instrument.Name, instrument.Group, instrument.AssetClass, instrument.Region, instrument.IsVolatilityIndex);
}
