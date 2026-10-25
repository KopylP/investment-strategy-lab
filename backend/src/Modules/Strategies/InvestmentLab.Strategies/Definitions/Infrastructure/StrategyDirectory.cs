using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken) =>
        [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId))];
}
