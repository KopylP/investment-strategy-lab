using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Portfolio.Transactions.Application.UploadStatement;

public sealed class UploadStatementHandler(
    ITransactionJournal journal,
    IStatementImportRepository imports,
    IStatementParser parser,
    StatementPreview preview,
    IInstrumentCatalog catalog,
    IInstrumentRegistry registry,
    IStrategyDirectory strategies,
    IFxRateBackfill fxBackfill,
    TimeProvider clock)
{
    public async Task<Result<StatementImportDto>> Handle(Guid accountId, string fileName, string content, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var parsed = parser.Parse(content);
        if (parsed.IsFailure)
            return parsed.Error;

        var statement = StatementImport.Upload(account.Id, account.OwnerId, fileName, parsed.Value, clock.GetUtcNow());

        await LoadRatesForForeignRowsAsync(account, statement, cancellationToken);

        var mappings = await SuggestMappingsAsync(account, statement.Rows, cancellationToken);
        statement.Configure(ImportMode.Merge, null, SuggestCashSubPortfolio(account, statement.Rows, mappings), mappings);
        imports.Add(statement);

        return await preview.PlanAndShowAsync(account, statement, cancellationToken);
    }

    private async Task LoadRatesForForeignRowsAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var anyForeign = statement.Rows.Any(row => !string.Equals(row.Currency, account.BaseCurrency.Code, StringComparison.OrdinalIgnoreCase));

        if (anyForeign)
            await fxBackfill.EnsureSinceAsync(statement.Rows.Min(row => row.OccurredOn), cancellationToken);
    }

    private async Task<List<TickerMapping>> SuggestMappingsAsync(
        InvestmentAccount account, IReadOnlyList<StatementRow> rows, CancellationToken cancellationToken)
    {
        var instruments = await catalog.GetInstruments(cancellationToken);
        var strategiesKnown = await strategies.GetStrategies(cancellationToken);
        var universes = strategiesKnown.ToDictionary(strategy => strategy.Id, strategy => strategy.Universe);

        var tickers = await TickersOfAsync(rows, cancellationToken);
        var listed = await ListedElsewhereAsync(tickers, cancellationToken);

        return [.. tickers.Select(ticker => MappingOf(ticker, listed.GetValueOrDefault(ticker.Ticker), instruments, account, universes))];
    }

    private async Task<List<StatementTicker>> TickersOfAsync(IReadOnlyList<StatementRow> rows, CancellationToken cancellationToken)
    {
        var groups = rows.Where(row => row.NeedsInstrument && row.Ticker is not null).GroupBy(row => row.Ticker!, StringComparer.OrdinalIgnoreCase);
        var tickers = groups.Select(group => new BrokerTickerDto(group.Key.ToUpperInvariant(), group.First().Currency)).ToList();

        var matches = await registry.MatchAsync(tickers, cancellationToken);

        return [.. tickers.Zip(matches, (ticker, match) => new StatementTicker(ticker.Ticker, ticker.Currency, match.Instrument, match.SuggestedSymbol))];
    }

    private async Task<Dictionary<string, ListedSymbolDto?>> ListedElsewhereAsync(List<StatementTicker> tickers, CancellationToken cancellationToken)
    {
        var unknown = tickers.Where(ticker => ticker.Match is null).ToList();
        var found =
            await registry.ResolveAsync([.. unknown.Select(ticker => new BrokerTickerDto(ticker.Ticker, ticker.Currency))], cancellationToken);

        return unknown.Zip(found).ToDictionary(pair => pair.First.Ticker, pair => pair.Second);
    }

    private static TickerMapping MappingOf(
        StatementTicker ticker,
        ListedSymbolDto? listing,
        IReadOnlyList<InstrumentDto> instruments,
        InvestmentAccount account,
        Dictionary<Guid, IReadOnlyList<Guid>> universes)
    {
        var match = ticker.Match ?? InstrumentOfListing(listing, instruments);
        var sub = SubPortfolioFor(match, account, universes);

        if (match is not null)
            return new TickerMapping(ticker.Ticker, ticker.Currency, match.Id, null, sub);

        return listing is null
            ? new TickerMapping(ticker.Ticker, ticker.Currency, null, ticker.SuggestedSymbol, sub)
            : new TickerMapping(ticker.Ticker, ticker.Currency, null, listing.Symbol, sub, listing.Name);
    }

    private static InstrumentDto? InstrumentOfListing(ListedSymbolDto? listing, IReadOnlyList<InstrumentDto> instruments) =>
        listing is null
            ? null
            : instruments.FirstOrDefault(instrument => string.Equals(instrument.Ticker, listing.Symbol, StringComparison.OrdinalIgnoreCase));

    private static Guid? SubPortfolioFor(InstrumentDto? match, InvestmentAccount account, Dictionary<Guid, IReadOnlyList<Guid>> universes)
    {
        if (account.ActiveSubPortfolios.ToList() is [var only])
            return only.Id.Value;

        if (match is null)
            return null;

        var holder = account.ActiveSubPortfolios.FirstOrDefault(sub =>
            sub.Strategy is { } strategy && universes.TryGetValue(strategy.StrategyId, out var universe) && universe.Contains(match.Id));

        return holder?.Id.Value;
    }

    private static Guid? SuggestCashSubPortfolio(InvestmentAccount account, IReadOnlyList<StatementRow> rows, IReadOnlyList<TickerMapping> mappings)
    {
        var mostSpentOn = rows.Where(row => row.Kind == StatementRowKind.Buy)
            .Select(row => (
                Sub: mappings.FirstOrDefault(mapping => string.Equals(
                    mapping.Ticker,
                    row.Ticker,
                    StringComparison.OrdinalIgnoreCase))?.SubPortfolioId,
                row.Amount))
            .Where(entry => entry.Sub is not null)
            .GroupBy(entry => entry.Sub!.Value)
            .OrderByDescending(group => group.Sum(entry => entry.Amount))
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();

        return mostSpentOn ?? account.ActiveSubPortfolios.FirstOrDefault()?.Id.Value;
    }

    private sealed record StatementTicker(string Ticker, string Currency, InstrumentDto? Match, string SuggestedSymbol);
}
