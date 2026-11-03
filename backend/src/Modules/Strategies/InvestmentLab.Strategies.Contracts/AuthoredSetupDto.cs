namespace InvestmentLab.Strategies.Contracts;

public sealed record AuthoredSetupDto(
    string TypeCode,
    string Parameters,
    IReadOnlyList<Guid> Universe,
    Guid BenchmarkInstrumentId,
    Guid? CashInstrumentId,
    Guid? VolatilityIndexInstrumentId,
    IReadOnlyList<Guid> ExtraSeries,
    IReadOnlyList<AuthoredReportDto> Reports);
