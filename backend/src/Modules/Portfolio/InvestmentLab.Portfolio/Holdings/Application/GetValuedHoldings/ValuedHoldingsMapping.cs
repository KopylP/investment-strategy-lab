using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.GetValuedHoldings;

internal static class ValuedHoldingsMapping
{
    private const int MoneyDecimals = 2;
    private const int PriceDecimals = 6;
    private const int ShareDecimals = 6;

    public static ValuedHoldingsDto ToDto(this ValuedHoldings valued, InvestmentAccount account) => new(
        AccountId: valued.Holdings.AccountId.Value,
        Currency: valued.Holdings.Currency.Code,
        AsOf: valued.Holdings.AsOf,
        Value: MoneyOf(valued.Value),
        Cash: MoneyOf(valued.Cash),
        CostBasis: MoneyOf(valued.CostBasis),
        Result: MoneyOf(valued.Result),
        ResultShare: ShareOf(valued.ResultShare),
        SubPortfolios: [.. valued.SubPortfolios.Select(sub => sub.ToDto(account, valued.Holdings.Currency))]);

    private static ValuedSubPortfolioDto ToDto(this ValuedSubPortfolio sub, InvestmentAccount account, Currency currency) => new(
        SubPortfolioId: sub.Holdings.SubPortfolioId.Value,
        Name: NameOf(account, sub.Holdings.SubPortfolioId),
        Cash: MoneyOf(sub.Cash),
        CashBalances: CashBalancesOf(sub.Holdings, currency),
        Value: MoneyOf(sub.Value),
        CostBasis: MoneyOf(sub.CostBasis),
        Result: MoneyOf(sub.Result),
        Share: ShareOf(sub.Share),
        Positions: [.. sub.Positions.Select(ToDto)]);

    private static ValuedPositionDto ToDto(ValuedPosition position) => new(
        InstrumentId: position.Line.InstrumentId,
        Quantity: position.Line.Quantity.Value,
        AverageCost: decimal.Round(position.Line.AverageCost, PriceDecimals),
        CostBasis: position.CostBasis,
        Price: PriceOf(position.Price?.InAccountCurrency),
        QuotedPrice: position.Price?.Quoted,
        QuoteCurrency: position.Price?.QuoteCurrency.Code,
        PriceDate: position.Price?.Date,
        IsStale: position.Price?.IsStale ?? true,
        Value: MoneyOf(position.Value),
        Result: MoneyOf(position.Result),
        ResultShare: ShareOf(position.ResultShare),
        Weight: ShareOf(position.Weight),
        Lots: [.. position.Line.Lots.Select(LotOf)]);

    private static IReadOnlyList<CashBalanceDto> CashBalancesOf(SubPortfolioHoldings holdings, Currency currency)
    {
        var own = new CashBalanceDto(currency.Code, holdings.Cash.Amount);
        var foreign = holdings.ForeignCash.Select(money => new CashBalanceDto(money.Currency.Code, MoneyOf(money.Amount)));

        return [own, .. foreign];
    }

    private static LotDto LotOf(VirtualLot lot) =>
        new(lot.OpenedBy.Value, lot.OpenedOn, lot.Quantity.Value, decimal.Round(lot.UnitCost, 8));

    private static string NameOf(InvestmentAccount account, SubPortfolioId id) =>
        account.SubPortfolios.FirstOrDefault(sub => sub.Id == id)?.Name ?? "";

    private static decimal MoneyOf(decimal amount) => decimal.Round(amount, MoneyDecimals);

    private static decimal? PriceOf(decimal? price) => price is { } value ? decimal.Round(value, PriceDecimals) : null;

    private static decimal ShareOf(decimal share) => decimal.Round(share, ShareDecimals);

    private static decimal? ShareOf(decimal? share) => share is { } value ? ShareOf(value) : null;
}
