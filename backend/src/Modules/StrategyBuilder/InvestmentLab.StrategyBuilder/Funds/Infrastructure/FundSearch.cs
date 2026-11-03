using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.StrategyBuilder.Funds.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.StrategyBuilder.Funds.Infrastructure;

internal sealed class FundSearch(IFundScreen screen, IServiceScopeFactory scopes, ILogger<FundSearch> logger) : IFundSearch
{
    public FundSearchOptions Options()
    {
        var options = screen.Options();
        var exchanges = options.Exchanges.Select(ChoiceOf).ToList();
        var regions = options.Regions.Select(ChoiceOf).ToList();

        return new FundSearchOptions(exchanges, regions);
    }

    public async Task<Result<IReadOnlyList<FoundFund>>> SearchAsync(FundCriteria criteria, CancellationToken cancellationToken)
    {
        var query = new FundScreenCriteriaDto(
            ExchangeId: criteria.ExchangeId,
            RegionId: criteria.RegionId,
            FundFamily: criteria.FundFamily,
            MinAssets: criteria.MinAssets,
            MaxExpenseRatio: criteria.MaxExpenseRatio,
            NameWords: criteria.NameWords);
        var result = await screen.ScreenAsync(query, cancellationToken);

        if (result.ErrorCode is not null)
            return FundErrors.SearchUnavailable();

        var funds = result.Funds.Select(FundOf).ToList();

        return Result<IReadOnlyList<FoundFund>>.Success(funds);
    }

    public async Task<AddedFunds> AddAsync(IReadOnlyList<FoundFund> funds, CancellationToken cancellationToken)
    {
        var added = new AddedFunds([], []);

        foreach (var fund in funds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            added = await AddFundAsync(added, fund, cancellationToken);
        }

        return added;
    }

    private async Task<AddedFunds> AddFundAsync(AddedFunds added, FoundFund fund, CancellationToken cancellationToken)
    {
        var registration = NewInstrumentOf(fund);
        if (registration.IsFailure)
            return added.With(fund.Symbol, null, registration.Error.AsMessage());

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var registry = scope.ServiceProvider.GetRequiredService<IInstrumentRegistry>();
            var result = await registry.RegisterAsync(registration.Value, cancellationToken);

            return added.With(fund.Symbol, result.InstrumentId, result.Error);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not register fund {Symbol}", fund.Symbol);

            return added.With(fund.Symbol, null, FundErrors.RegistrationFailed().AsMessage());
        }
    }

    private static Result<NewInstrumentDto> NewInstrumentOf(FoundFund fund)
    {
        if (string.IsNullOrWhiteSpace(fund.Symbol) || string.IsNullOrWhiteSpace(fund.Name) || string.IsNullOrWhiteSpace(fund.Currency))
            return FundErrors.InvalidFund();

        return new NewInstrumentDto(fund.Symbol, fund.Name, fund.Currency, "Etf");
    }

    private static FundSearchChoice ChoiceOf(ScreenChoiceDto choice) => new(choice.Id, choice.Label);

    private static FoundFund FundOf(ScreenedFundDto fund) => new(
        Symbol: fund.Symbol,
        Name: fund.Name,
        Currency: fund.Currency,
        FundFamily: fund.FundFamily,
        TotalAssets: fund.TotalAssets,
        ExpenseRatio: fund.ExpenseRatio,
        InstrumentId: fund.RegisteredInstrumentId);
}
