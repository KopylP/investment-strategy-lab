namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record InstrumentGroup
{
    public string Name { get; }

    private InstrumentGroup(string name) => Name = name;

    public static InstrumentGroup Of(string name) =>
        TryOf(name) is { IsSuccess: true } result ? result.Value : throw new ArgumentException("Blank instrument group.", nameof(name));

    public static Result<InstrumentGroup> TryOf(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? Error.Validation("instrument_group.blank", "Instrument group must not be blank.")
            : new InstrumentGroup(name.Trim());

    public override string ToString() => Name;
}
