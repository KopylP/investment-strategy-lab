using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ConfigureStatementImport;

public sealed class ConfigureStatementImportHandler(StatementImportLoader loader, StatementPreview preview, IInstrumentRegistry registry)
{
    public async Task<Result<StatementImportDto>> Handle(
        Guid accountId, Guid importId, ConfigureImportCommand command, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (account, statement) = loaded.Value;

        if (!Enum.TryParse<ImportMode>(command.Mode, ignoreCase: true, out var mode))
            return Error.Validation("statement_import.mode_invalid", $"'{command.Mode}' is not Merge, UpdateImported, Replace or FromScratch.");

        var mappings = statement.Mappings.Select(mapping => Applied(mapping, command.Mappings)).ToList();
        await NameTypedSymbolsAsync(statement.Mappings, mappings, cancellationToken);

        var configured = statement.Configure(mode, command.FromDate, command.CashSubPortfolioId, mappings);
        if (configured.IsFailure)
            return configured.Error;

        return await preview.PlanAndShowAsync(account, statement, cancellationToken);
    }

    private static TickerMapping Applied(TickerMapping mapping, IReadOnlyList<TickerMappingInput> inputs)
    {
        var input = inputs.FirstOrDefault(each => string.Equals(each.Ticker, mapping.Ticker, StringComparison.OrdinalIgnoreCase));
        if (input is null)
            return mapping;

        var symbol = input.InstrumentId is null ? input.RegisterAs?.Trim().ToUpperInvariant() : null;

        return mapping with
        {
            InstrumentId = input.InstrumentId,
            RegisterAs = symbol,
            SubPortfolioId = input.SubPortfolioId,
            RegisterName = symbol == mapping.RegisterAs ? mapping.RegisterName : null,
        };
    }

    private async Task NameTypedSymbolsAsync(IReadOnlyList<TickerMapping> before, List<TickerMapping> mappings, CancellationToken cancellationToken)
    {
        var typed = Enumerable.Range(0, mappings.Count)
            .Where(i => mappings[i].RegisterAs is not null && mappings[i].RegisterAs != before[i].RegisterAs)
            .ToList();
        var tickers = typed.Select(i => new BrokerTickerDto(mappings[i].RegisterAs!, mappings[i].Currency, IsExactSymbol: true));
        var found = await registry.ResolveAsync([.. tickers], cancellationToken);

        foreach (var (i, listed) in typed.Zip(found))
            mappings[i] = mappings[i] with { RegisterName = listed?.Name };
    }
}
