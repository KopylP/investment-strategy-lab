using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.GetHoldings;

internal static class HoldingsMapping
{
    extension(AccountHoldings holdings)
    {
        public AccountHoldingsDto ToDto(InvestmentAccount account) => new(
            holdings.AccountId.Value,
            holdings.Currency.Code,
            holdings.AsOf,
            [.. holdings.SubPortfolios.Select(sub => new SubPortfolioHoldingsDto(
                sub.SubPortfolioId.Value,
                account.SubPortfolios.FirstOrDefault(s => s.Id == sub.SubPortfolioId)?.Name ?? "",
                sub.Cash.Amount,
                [.. sub.Lines.Select(line => new PositionDto(
                    line.InstrumentId,
                    line.Quantity.Value,
                    line.CostBasis,
                    decimal.Round(line.AverageCost, 6),
                    line.FirstOpenedOn,
                    [.. line.Lots.Select(lot => new LotDto(lot.OpenedBy.Value, lot.OpenedOn, lot.Quantity.Value, decimal.Round(lot.UnitCost, 8)))]))],
                [.. sub.ForeignCash.Select(money => new CashBalanceDto(money.Currency.Code, decimal.Round(money.Amount, 2)))]))]);
    }
}
