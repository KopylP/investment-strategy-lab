using System.Text.Json;
using System.Text.Json.Serialization;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Instruments.Infrastructure.Seed;

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
            if (await instruments.FindOwnerAsync(ProviderSymbol.TryParse(command.ProviderSymbols[0]).Value, cancellationToken) is { } owner)
            {
                await FillMissingExposureAsync(owner, command, cancellationToken);
                continue;
            }

            var result = await register.Handle(command, cancellationToken);
            if (result.IsFailure)
                logger.LogWarning("Seed instrument {Symbol} skipped: {Error}", command.ProviderSymbols[0], result.Error.Message);
        }
    }

    private async Task FillMissingExposureAsync(InstrumentId owner, RegisterInstrumentCommand command, CancellationToken cancellationToken)
    {
        var instrument = await instruments.FindAsync(owner, cancellationToken);
        if (instrument is null || instrument.AssetClass is not null)
            return;

        if (InstrumentExposure.TryOf(command.AssetClass, command.Region, command.AssetCurrency).Value is not { } exposure)
            return;

        instrument.AssignExposure(exposure);
        await instruments.SaveChangesAsync(cancellationToken);
    }
}
