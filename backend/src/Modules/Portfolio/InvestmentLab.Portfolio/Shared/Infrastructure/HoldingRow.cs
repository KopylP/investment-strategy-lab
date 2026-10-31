using System.Text.Json;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

public sealed class HoldingRow
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public Guid SubPortfolioId { get; private set; }
    public Guid? InstrumentId { get; private set; }
    public decimal Quantity { get; private set; }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; } = null!;
    public string Lots { get; private set; } = "[]";
    public DateOnly? AsOf { get; private set; }

    public static IEnumerable<HoldingRow> From(AccountHoldings holdings, UserId ownerId)
    {
        foreach (var sub in holdings.SubPortfolios)
        {
            yield return New(holdings, ownerId, sub.SubPortfolioId, null, 0m, sub.Cash.Amount, "[]");

            foreach (var foreign in sub.ForeignCash)
                yield return New(holdings, ownerId, sub.SubPortfolioId, null, 0m, foreign.Amount, "[]", foreign.Currency);

            foreach (var line in sub.Lines)
                yield return New(holdings, ownerId, sub.SubPortfolioId, line.InstrumentId, line.Quantity.Value, line.CostBasis,
                    JsonSerializer.Serialize(
                        line.Lots.Select(lot => new LotJson(lot.OpenedBy.Value, lot.OpenedOn, lot.Quantity.Value, lot.UnitCost)),
                        Json));
        }
    }

    public static AccountHoldings ToHoldings(InvestmentAccount account, IReadOnlyList<HoldingRow> rows) => new(
        account.Id,
        account.BaseCurrency,
        rows.Select(row => row.AsOf).FirstOrDefault(),

        [.. rows.GroupBy(row => row.SubPortfolioId)
            .OrderBy(sub => account.SubPortfolios.ToList().FindIndex(s => s.Id.Value == sub.Key))
            .Select(sub => new SubPortfolioHoldings(
            Accounts.Domain.SubPortfolioId.Of(sub.Key),
            Money.Of(sub.Where(row => row.InstrumentId is null && row.Currency == account.BaseCurrency).Sum(row => row.Amount), account.BaseCurrency),
            [.. sub.Where(row => row.InstrumentId is null && row.Currency != account.BaseCurrency)
                .OrderBy(row => row.Currency.Code)
                .Select(row => Money.Of(row.Amount, row.Currency))],
            [.. sub.Where(row => row.InstrumentId is not null).OrderBy(row => row.InstrumentId).Select(row => new HoldingLine(
                row.InstrumentId!.Value,
                [.. JsonSerializer.Deserialize<List<LotJson>>(row.Lots, Json)!.Select(lot =>
                    new VirtualLot(
                        TransactionId.Of(lot.OpenedBy),
                        lot.OpenedOn,
                        InvestmentLab.SharedKernel.Quantity.Of(lot.Quantity),
                        lot.UnitCost))]))]))]);

    private static HoldingRow New(
        AccountHoldings holdings,
        UserId ownerId,
        SubPortfolioId sub,
        Guid? instrumentId,
        decimal quantity,
        decimal amount,
        string lots,
        Currency? currency = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AccountId = holdings.AccountId.Value,
            OwnerId = ownerId,
            SubPortfolioId = sub.Value,
            InstrumentId = instrumentId,
            Quantity = quantity,
            Amount = amount,
            Currency = currency ?? holdings.Currency,
            Lots = lots,
            AsOf = holdings.AsOf,
        };

    private sealed record LotJson(Guid OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
}
