using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;

public sealed record InstrumentChoiceDto(Guid Id, string Label, string Name, string? Group, string? AssetClass, string? Region, bool IsVolatilityIndex);

public sealed class ListInstrumentsHandler(IInstrumentDirectory instruments)
{
    public async Task<IReadOnlyList<InstrumentChoiceDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await instruments.AllAsync(cancellationToken);

        return [.. all.Select(ToDto)];
    }

    private static InstrumentChoiceDto ToDto(DirectoryInstrument instrument) =>
        new(instrument.Id, instrument.Label, instrument.Name, instrument.Group, instrument.AssetClass, instrument.Region, instrument.IsVolatilityIndex);
}
