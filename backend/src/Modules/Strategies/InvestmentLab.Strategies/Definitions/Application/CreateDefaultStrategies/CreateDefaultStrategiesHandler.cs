using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;

public sealed class CreateDefaultStrategiesHandler(
    IDefaultStrategies defaults, IInstrumentCatalog instruments, CreateStrategyHandler create, IStrategyRepository strategies) : IHandler
{
    private const string VolatilityIndexName = "volatility-index";

    public async Task<Result<IReadOnlyList<StrategyDto>>> Handle(UserId ownerId, CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);
        var byIsin = known.Where(instrument => instrument.Isin is not null).ToDictionary(instrument => instrument.Isin!, instrument => instrument.Id);
        var volatilityIndex = known.FirstOrDefault(instrument => instrument.IsVolatilityIndex)?.Id;

        var created = new List<StrategyDto>();

        foreach (var strategy in defaults.Load())
        {
            if (await strategies.NameTakenAsync(ownerId, strategy.Name, cancellationToken))
                continue;

            var missing = MissingInstruments(strategy, byIsin, volatilityIndex);
            if (missing.Count > 0)
                return StrategyErrors.UnknownInstruments(missing);

            var result = await create.Handle(ownerId, CommandOf(strategy, byIsin, volatilityIndex), cancellationToken);
            if (result.IsFailure)
                return result.Error;

            created.Add(result.Value);
        }

        return created;
    }

    private static List<string> MissingInstruments(DefaultStrategy strategy, Dictionary<string, Guid> byIsin, Guid? volatilityIndex)
    {
        string?[] isins = [.. strategy.Universe, strategy.Benchmark, strategy.CashInstrument];
        var missing = isins.OfType<string>().Where(isin => !byIsin.ContainsKey(isin)).ToList();

        if (strategy.UsesVolatilityIndex && volatilityIndex is null)
            missing.Add(VolatilityIndexName);

        return missing;
    }

    private static CreateStrategyCommand CommandOf(DefaultStrategy strategy, Dictionary<string, Guid> byIsin, Guid? volatilityIndex)
    {
        var setup = new StrategySetupDraft(
            [.. strategy.Universe.Select(isin => byIsin[isin])],
            byIsin[strategy.Benchmark],
            strategy.UsesVolatilityIndex ? volatilityIndex : null,
            strategy.CashInstrument is { } cash ? byIsin[cash] : null,
            strategy.Parameters);

        return new CreateStrategyCommand(strategy.Name, strategy.TypeCode, setup);
    }
}
