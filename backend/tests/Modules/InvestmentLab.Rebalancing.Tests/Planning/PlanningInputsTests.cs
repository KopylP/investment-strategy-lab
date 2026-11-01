using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class PlanningInputsTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class HoldingForeignCash(AccountHoldingsDto holdings) : IHoldingsQueries
    {
        public Task<AccountHoldingsDto?> GetHoldings(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken) =>
            Task.FromResult<AccountHoldingsDto?>(holdings);

        public Task<SubPortfolioHoldingsDto?> GetSubPortfolioSnapshot(Guid accountId, Guid subPortfolioId, DateOnly? asOf, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingRates : IFxRates
    {
        public List<DateOnly> AskedDates { get; } = [];

        public Task<FxRateDto?> GetFxRate(string baseCurrency, string quoteCurrency, DateOnly transactionDate, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(string baseCurrency, string quoteCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<decimal?> GetCrossRate(string fromCurrency, string toCurrency, DateOnly asOf, CancellationToken cancellationToken)
        {
            AskedDates.Add(asOf);

            return Task.FromResult<decimal?>(4m);
        }

        public Task<IReadOnlyList<CrossRateDto>> GetCrossRateHistory(string fromCurrency, string toCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Foreign_cash_is_converted_at_the_rate_of_the_injected_clock()
    {
        var account = new AccountSetupDto(Guid.NewGuid(), Guid.NewGuid(), "Main", "Broker", "PLN", "none", "none", 0m, []);
        var sub = new SubPortfolioHoldingsDto(Guid.NewGuid(), "Sub", 10m, [], [new CashBalanceDto("EUR", 100m)]);
        var rates = new RecordingRates();
        var clock = new FixedClock(new DateTimeOffset(2020, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var inputs = new PlanningInputs(null!, new HoldingForeignCash(new AccountHoldingsDto(account.Id, "PLN", null, [sub])), null!, null!, rates, null!, clock);

        var holdings = await inputs.HoldingsAsync(account, TestContext.Current.CancellationToken);

        holdings.SubPortfolios[0].Cash.ShouldBe(410m);
        rates.AskedDates.ShouldBe([new DateOnly(2020, 3, 11)]);
    }
}
