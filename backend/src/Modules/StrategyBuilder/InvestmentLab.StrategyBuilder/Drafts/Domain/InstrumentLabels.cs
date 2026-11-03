namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record InstrumentLabels(IReadOnlyDictionary<Guid, string> Labels)
{
    private const int ShortIdLength = 8;

    public static InstrumentLabels None { get; } = new(new Dictionary<Guid, string>());

    public string Of(Guid id) => Labels.TryGetValue(id, out var label) ? label : id.ToString("N")[..ShortIdLength];
}
