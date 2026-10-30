using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application;

public interface IStrategyRepository
{
    Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken cancellationToken);

    Task<bool> NameTakenAsync(UserId ownerId, string name, CancellationToken cancellationToken);

    void Add(Strategy strategy);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Strategy types of the catalog (synced from the engines when missing or stale).</summary>
public interface IStrategyTypes
{
    Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken);
}

/// <summary>Strategies every user can start from, taken over from the Python flow's TOML files.</summary>
public interface IDefaultStrategies
{
    IReadOnlyList<DefaultStrategy> Load();
}

/// <param name="Universe">Yahoo symbols; resolved to instruments when the strategy is created.</param>
public sealed record DefaultStrategy(
    string Name,
    string TypeCode,
    string Currency,
    IReadOnlyList<string> Universe,
    string? CashInstrument,
    string Benchmark,
    string? VolatilityIndex,
    JsonElement Parameters);
