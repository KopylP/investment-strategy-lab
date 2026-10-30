using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;

/// <summary>Gives the user the EU and US momentum strategies of the Python flow; ones they already have (by name) are skipped.</summary>
public sealed class CreateDefaultStrategiesHandler(
    IDefaultStrategies defaults, IInstrumentCatalog instruments, CreateStrategyHandler create, IStrategyRepository strategies)
{
    private const string YahooPrefix = "yfinance:";

    public async Task<Result<IReadOnlyList<StrategyDto>>> Handle(UserId ownerId, CancellationToken cancellationToken)
    {
        var bySymbol = (await instruments.GetInstruments(cancellationToken))
            .SelectMany(instrument => instrument.ProviderSymbols
                .Where(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal))
                .Select(symbol => (Symbol: symbol[YahooPrefix.Length..], instrument.Id)))
            .ToDictionary(pair => pair.Symbol, pair => pair.Id);

        var created = new List<StrategyDto>();
        foreach (var strategy in defaults.Load())
        {
            if (await strategies.NameTakenAsync(ownerId, strategy.Name, cancellationToken))
                continue;

            string?[] symbols = [.. strategy.Universe, strategy.Benchmark, strategy.VolatilityIndex, strategy.CashInstrument];
            var missing = symbols.OfType<string>().Where(symbol => !bySymbol.ContainsKey(symbol)).ToList();
            if (missing.Count > 0)
                return StrategyErrors.UnknownInstruments(missing);

            var result = await create.Handle(ownerId, new CreateStrategyCommand(strategy.Name, strategy.TypeCode, new StrategySetupInput(
                [.. strategy.Universe.Select(symbol => bySymbol[symbol])],
                bySymbol[strategy.Benchmark],
                strategy.VolatilityIndex is { } vix ? bySymbol[vix] : null,
                strategy.CashInstrument is { } cash ? bySymbol[cash] : null,
                strategy.Parameters)), cancellationToken);
            if (result.IsFailure)
                return result.Error;
            created.Add(result.Value);
        }

        return created;
    }
}
