using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.GetHoldings;

public interface IHoldingsReader
{
    Task<InvestmentAccount?> FindAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>The stored read model, kept current by every recorded entry.</summary>
    Task<AccountHoldings> GetCurrentAsync(InvestmentAccount account, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetEntriesAsync(AccountId accountId, CancellationToken cancellationToken);
}

public sealed class GetHoldingsHandler(IHoldingsReader reader)
{
    /// <param name="asOf">Null: current holdings from the read model; a date: the journal replayed up to it.</param>
    public async Task<Result<AccountHoldingsDto>> Handle(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await reader.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (asOf is null)
            return (await reader.GetCurrentAsync(account, cancellationToken)).ToDto(account);

        var projected = HoldingsProjector.Project(account, await reader.GetEntriesAsync(account.Id, cancellationToken), asOf);
        return projected.IsSuccess ? projected.Value.Holdings.ToDto(account) : projected.Error;
    }
}

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
                    [.. line.Lots.Select(lot => new LotDto(lot.OpenedBy.Value, lot.OpenedOn, lot.Quantity.Value, decimal.Round(lot.UnitCost, 8)))]))]))]);
    }
}
