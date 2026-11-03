namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed class StrategyVersion
{
    public Guid Id { get; private set; }
    public int Number { get; private set; }
    public string Parameters { get; private set; }
    public List<Guid> Universe { get; private set; }
    public Guid? CashInstrumentId { get; private set; }
    public Guid BenchmarkInstrumentId { get; private set; }
    public Guid? VolatilityIndexInstrumentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public List<VersionReport> Reports { get; private set; } = [];

    public List<Guid> ExtraSeries { get; private set; } = [];

    private StrategyVersion(int number, StrategySetup setup, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Number = number;
        Parameters = setup.Parameters;
        Universe = [.. setup.Universe];
        CashInstrumentId = setup.CashInstrumentId;
        BenchmarkInstrumentId = setup.Market.BenchmarkInstrumentId;
        VolatilityIndexInstrumentId = setup.Market.VolatilityIndexInstrumentId;
        CreatedAt = createdAt;
        Reports = [.. setup.Reports ?? []];
        ExtraSeries = [.. setup.ExtraSeries ?? []];
    }

    private StrategyVersion() => (Parameters, Universe) = (null!, null!);

    internal static StrategyVersion Create(int number, StrategySetup setup, DateTimeOffset createdAt) => new(number, setup, createdAt);
}
