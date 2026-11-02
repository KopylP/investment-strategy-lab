namespace InvestmentLab.Taxation.Contracts;

public interface ITaxSimulation
{
    Task<SimulatedTaxDto> SimulateAsync(IReadOnlyList<SimulatedTradeDto> trades, CancellationToken cancellationToken);
}
