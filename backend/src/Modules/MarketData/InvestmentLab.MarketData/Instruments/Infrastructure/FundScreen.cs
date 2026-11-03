using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class FundScreen(IFundScreener screener, IInstrumentRepository instruments) : IFundScreen
{
    public FundScreenOptionsDto Options()
    {
        var options = screener.Options();
        var exchanges = options.Exchanges.Select(ChoiceOf).ToList();
        var regions = options.Regions.Select(ChoiceOf).ToList();

        return new FundScreenOptionsDto(exchanges, regions);
    }

    public async Task<FundScreenResultDto> ScreenAsync(FundScreenCriteriaDto criteria, CancellationToken cancellationToken)
    {
        var query = new FundScreenQuery(
            Exchange: criteria.ExchangeId,
            Region: criteria.RegionId,
            FundFamily: criteria.FundFamily,
            MinAssets: criteria.MinAssets,
            MaxExpenseRatio: criteria.MaxExpenseRatio,
            NameWords: criteria.NameWords);
        var matches = await screener.ScreenAsync(query, cancellationToken);

        if (matches is null)
            return new FundScreenResultDto([], "market_data.fund_screen.unavailable");

        var known = await instruments.ListAsync(cancellationToken);
        var funds = matches.Select(match => MatchOf(match, known)).ToList();

        return new FundScreenResultDto(funds, null);
    }

    private static ScreenChoiceDto ChoiceOf(FundScreenChoice choice) => new(choice.Id, choice.Label);

    private static ScreenedFundDto MatchOf(FundScreenMatch match, IReadOnlyList<Instrument> known)
    {
        var instrument = known.FirstOrDefault(item => HasSymbol(item, match.Symbol));

        return new ScreenedFundDto(
            Symbol: match.Symbol,
            Name: match.Name,
            Currency: match.Currency,
            Exchange: match.Exchange,
            FundFamily: match.FundFamily,
            TotalAssets: match.TotalAssets,
            ExpenseRatio: match.ExpenseRatio,
            RegisteredInstrumentId: instrument?.Id.Value);
    }

    private static bool HasSymbol(Instrument instrument, string symbol)
    {
        var registered = instrument.SymbolFor(QuoteProvider.Primary);

        return registered is not null && string.Equals(registered.Symbol, symbol, StringComparison.OrdinalIgnoreCase);
    }
}
