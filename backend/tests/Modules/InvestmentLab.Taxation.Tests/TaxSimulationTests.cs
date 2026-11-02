using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Taxation.Assessments.Application.SimulateTax;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Taxation.Tests;

public sealed class TaxSimulationTests
{
    private static readonly Guid Fund = Guid.NewGuid();
    private static readonly DateOnly Bought = new(2024, 3, 1);
    private static readonly DateOnly Sold = new(2024, 6, 3);

    [Fact]
    public async Task A_simulated_sale_is_taxed_in_zloty_at_the_rates_of_the_purchase_and_of_the_sale()
    {
        var simulation = new TaxSimulation(new FixedRates(new() { [Bought] = 4.3m, [Sold] = 4.4m }), new LoadedBackfill());
        SimulatedTradeDto[] trades =
        [
            new(Fund, Bought, IsSale: false, Quantity: 100m, Price: 10m, Fee: 0m, Currency: "EUR"),
            new(Fund, Sold, IsSale: true, Quantity: 100m, Price: 15m, Fee: 0m, Currency: "EUR"),
        ];

        var simulated = await simulation.SimulateAsync(trades, TestContext.Current.CancellationToken);

        simulated.TaxCurrency.ShouldBe("PLN");
        simulated.Years.Single().ShouldBe(new SimulatedTaxYearDto(2024, 437m));
    }

    [Fact]
    public async Task A_rate_that_cannot_be_found_fails_the_simulation()
    {
        var simulation = new TaxSimulation(new FixedRates([]), new LoadedBackfill());
        SimulatedTradeDto[] trades = [new(Fund, Bought, IsSale: false, Quantity: 1m, Price: 10m, Fee: 0m, Currency: "EUR")];

        await Should.ThrowAsync<InvalidOperationException>(() => simulation.SimulateAsync(trades, TestContext.Current.CancellationToken));
    }

    private sealed class FixedRates(Dictionary<DateOnly, decimal> rates) : ITaxCurrencyRates
    {
        public Task<ITaxCurrencyRate?> OnAsync(string currency, string taxCurrency, DateOnly lookupDate, CancellationToken cancellationToken) =>
            Task.FromResult<ITaxCurrencyRate?>(rates.TryGetValue(lookupDate, out var rate) ? new NbpRate(rate, lookupDate) : null);

        private sealed record NbpRate(decimal Rate, DateOnly EffectiveDate) : ITaxCurrencyRate;
    }

    private sealed class LoadedBackfill : IFxRateBackfill
    {
        public Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
