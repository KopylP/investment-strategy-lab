using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application;

public enum SeriesRole
{
    Instrument,
    Benchmark,
    VolatilityIndex,
}

public sealed record UniverseMember(Guid InstrumentId, string Name, string? Group);

public sealed record MarketSeries(Guid InstrumentId, SeriesRole Role, IReadOnlyList<QuotePointDto> Points);

/// <summary>Serialisable snapshot an engine decides on; it has no access to anything else.</summary>
public sealed record StrategyEvaluationContext(
    DateOnly AsOf,
    string Parameters,
    IReadOnlyList<UniverseMember> Universe,
    Guid? CashInstrumentId,
    IReadOnlyList<MarketSeries> MarketSeries,
    SubPortfolioStateDto SubPortfolio,
    decimal Contribution);

/// <summary>Runs a strategy type in its engine (Python today; pipelines, C# or AI later).</summary>
public interface IStrategyEngine
{
    Task<Result<StrategyDecisionDto>> DecideAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken);

    Task<Result<RiskAssessmentDto>> AssessRiskAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken);
}

public interface IStrategyReader
{
    Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken);
}

public static class EvaluationErrors
{
    public static Error EngineUnavailable(string details) =>
        Error.Conflict("strategy.engine_unavailable", $"The strategy engine did not answer: {details}");

    public static Error EngineRejected(string details) =>
        Error.Validation("strategy.engine_rejected", $"The strategy engine refused the context: {details}");
}
