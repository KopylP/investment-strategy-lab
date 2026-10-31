using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Definitions.Application;

public interface IStrategyTypes
{
    Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken);
}
