using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed class StatementPreview(
    ITransactionJournal journal,
    IStatementImportRepository imports,
    StatementPricing pricing,
    IInstrumentCatalog catalog)
{
    public async Task<StatementImportDto> PlanAndShowAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var current = await CurrentAsync(account, statement, cancellationToken);

        if (statement.IsPreview)
        {
            statement.Plan(current.Outcomes);
            await imports.SaveChangesAsync(cancellationToken);
        }

        return await DtoOfAsync(account, statement, current, cancellationToken);
    }

    public async Task<StatementImportDto> ShowAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var current = await CurrentAsync(account, statement, cancellationToken);

        return await DtoOfAsync(account, statement, current, cancellationToken);
    }

    private async Task<CurrentPlan> CurrentAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var priced = await pricing.PriceAsync(account, statement, cancellationToken);

        if (!statement.IsPreview)
            return new CurrentPlan(priced, statement.Outcomes, statement.ReversedTransactionIds.Count);

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);
        var outcomes = StatementMatcher.Match(priced, entries, statement.Mode, statement.FromDate);
        var toReverse = StatementMatcher.ToReverse(entries, statement.Mode).Count;

        return new CurrentPlan(priced, outcomes, toReverse);
    }

    private async Task<StatementImportDto> DtoOfAsync(
        InvestmentAccount account, StatementImport statement, CurrentPlan current, CancellationToken cancellationToken)
    {
        var known = await catalog.GetInstruments(cancellationToken);
        var names = known.ToDictionary(instrument => instrument.Id, instrument => instrument.Name);

        return new StatementImportDto(
            statement.Id,
            statement.Broker,
            statement.FileName,
            statement.UploadedAt,
            statement.Status.ToString(),
            statement.Mode.ToString(),
            statement.FromDate,
            statement.CashSubPortfolioId,
            account.BaseCurrency.Code,
            [.. statement.Mappings.Select(mapping => MappingDtoOf(mapping, statement, names))],
            RowsOf(statement, current.Outcomes, current.Priced),
            CountsOf(current.Outcomes, current.ToReverse),
            statement.Warnings,
            [.. account.ActiveSubPortfolios.Select(sub => new SubPortfolioOptionDto(sub.Id.Value, sub.Name))],
            statement.ConfirmedAt,
            statement.UndoneAt);
    }

    private static TickerMappingDto MappingDtoOf(TickerMapping mapping, StatementImport statement, Dictionary<Guid, string> names)
    {
        var rowCount = statement.Rows.Count(row => string.Equals(row.Ticker, mapping.Ticker, StringComparison.OrdinalIgnoreCase));
        var name = mapping.InstrumentId is { } id ? names.GetValueOrDefault(id) : mapping.RegisterName;

        return new TickerMappingDto(
            mapping.Ticker,
            mapping.Currency,
            mapping.InstrumentId,
            mapping.RegisterAs,
            mapping.SubPortfolioId,
            rowCount,
            name);
    }

    private static List<StatementRowDto> RowsOf(StatementImport statement, IReadOnlyList<RowOutcome> outcomes, IReadOnlyList<PricedRow> priced)
    {
        var outcomeOf = outcomes.ToDictionary(outcome => outcome.Number);
        var pricedOf = priced.ToDictionary(row => row.Row.Number);

        return [.. statement.Rows.Select(row => RowDtoOf(row, outcomeOf.GetValueOrDefault(row.Number), pricedOf.GetValueOrDefault(row.Number)))];
    }

    private static StatementRowDto RowDtoOf(StatementRow row, RowOutcome? outcome, PricedRow? priced) => new(
        row.Number,
        row.OccurredOn,
        row.Kind.ToString(),
        row.RawType,
        row.Ticker,
        row.Quantity,
        row.Price,
        row.Amount,
        row.Currency,
        priced is null ? row.Amount : decimal.Round(priced.Amount, 2),
        (outcome?.Decision ?? RowDecision.NeedsMapping).ToString(),
        outcome?.Note,
        outcome?.WillRecord ?? false,
        outcome?.WillReverseExisting ?? false,
        outcome?.Result);

    private static ImportCountsDto CountsOf(IReadOnlyList<RowOutcome> outcomes, int toReverse)
    {
        int Decided(RowDecision decision) => outcomes.Count(outcome => outcome.Decision == decision);

        return new ImportCountsDto(
            Decided(RowDecision.New),
            Decided(RowDecision.AlreadyImported),
            Decided(RowDecision.Differs),
            Decided(RowDecision.MatchesExisting),
            Decided(RowDecision.OutOfScope),
            Decided(RowDecision.Unsupported),
            Decided(RowDecision.NeedsMapping),
            outcomes.Count(outcome => outcome.WillRecord),
            toReverse + outcomes.Count(outcome => outcome.WillReverseExisting));
    }

    private sealed record CurrentPlan(IReadOnlyList<PricedRow> Priced, IReadOnlyList<RowOutcome> Outcomes, int ToReverse);
}
