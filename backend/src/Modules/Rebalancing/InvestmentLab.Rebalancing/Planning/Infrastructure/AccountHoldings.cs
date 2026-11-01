using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class AccountHoldings(IHoldingsQueries holdings, IFxRates fxRates, TimeProvider clock) : IAccountHoldings
{
    public async Task<IReadOnlyList<IHeldSubPortfolio>> OfAsync(Guid accountId, string accountCurrency, CancellationToken cancellationToken)
    {
        var current = await holdings.GetHoldings(accountId, null, cancellationToken);
        if (current is null)
            return [];

        var rateDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1);
        var rates = new Dictionary<string, decimal?>();
        var held = new List<IHeldSubPortfolio>();

        foreach (var sub in current.SubPortfolios)
        {
            var cash = await CashInAccountCurrencyAsync(sub, accountCurrency, rateDate, rates, cancellationToken);

            held.Add(new HeldSubPortfolio(sub, cash));
        }

        return held;
    }

    private async Task<decimal> CashInAccountCurrencyAsync(
        SubPortfolioHoldingsDto sub, string accountCurrency, DateOnly asOf, Dictionary<string, decimal?> rates, CancellationToken cancellationToken)
    {
        List<IForeignCash> foreign = [.. (sub.ForeignCash ?? []).Select(balance => new ForeignCash(balance))];

        foreach (var balance in foreign.Where(balance => !rates.ContainsKey(balance.Currency)))
            rates[balance.Currency] = await fxRates.GetCrossRate(balance.Currency, accountCurrency, asOf, cancellationToken);

        return AccountCash.Of(sub.Cash, foreign, rates);
    }

    private sealed class ForeignCash(CashBalanceDto dto) : IForeignCash
    {
        private readonly CashBalanceDto _dto = dto;

        public string Currency => _dto.Currency;
        public decimal Amount => _dto.Amount;
    }

    private sealed class HeldSubPortfolio(SubPortfolioHoldingsDto dto, decimal cashInAccountCurrency) : IHeldSubPortfolio
    {
        private readonly SubPortfolioHoldingsDto _dto = dto;

        public Guid Id => _dto.SubPortfolioId;
        public decimal Cash { get; } = cashInAccountCurrency;
        public IReadOnlyList<IHeldPosition> Positions { get; } = [.. dto.Positions.Select(position => new HeldPosition(position))];
    }

    private sealed class HeldPosition(PositionDto dto) : IHeldPosition
    {
        private readonly PositionDto _dto = dto;

        public Guid InstrumentId => _dto.InstrumentId;
        public decimal Quantity => _dto.Quantity;
        public decimal AverageCost => _dto.AverageCost;
        public IReadOnlyList<IHeldLot> Lots { get; } = [.. dto.Lots.Select(lot => new HeldLot(lot))];
    }

    private sealed class HeldLot(LotDto dto) : IHeldLot
    {
        private readonly LotDto _dto = dto;

        public decimal Quantity => _dto.Quantity;
        public decimal UnitCost => _dto.UnitCost;
        public DateOnly OpenedOn => _dto.OpenedOn;
    }
}
