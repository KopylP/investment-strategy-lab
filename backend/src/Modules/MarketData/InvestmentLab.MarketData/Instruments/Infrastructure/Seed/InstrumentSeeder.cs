using System.Text.Json;
using System.Text.Json.Serialization;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Instruments.Infrastructure.Seed;

/// <summary>
/// Registers the ETFs of the current strategies (strategy.toml, strategy_us.toml), their cash ETFs, ^VIX and the stocks
/// of the Revolut statement, so an import maps them straight away.
/// Instruments already known by their Yahoo symbol are left as they are.
/// </summary>
internal sealed class InstrumentSeeder(
    IInstrumentRepository instruments,
    RegisterInstrumentHandler register,
    ILogger<InstrumentSeeder> logger) : IModuleDataSeeder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static IReadOnlyList<RegisterInstrumentCommand> Load()
    {
        using var stream = typeof(InstrumentSeeder).Assembly.GetManifestResourceStream($"{typeof(InstrumentSeeder).Namespace}.instruments.json")
            ?? throw new InvalidOperationException("instruments.json is not embedded.");
        return JsonSerializer.Deserialize<List<RegisterInstrumentCommand>>(stream, Json) ?? [];
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var command in Load())
        {
            if (await instruments.FindOwnerAsync(ProviderSymbol.TryParse(command.ProviderSymbols[0]).Value, cancellationToken) is not null)
                continue;

            var result = await register.Handle(command, cancellationToken);
            if (result.IsFailure)
                logger.LogWarning("Seed instrument {Symbol} skipped: {Error}", command.ProviderSymbols[0], result.Error.Message);
        }
    }
}
