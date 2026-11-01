using InvestmentLab.MarketData.Contracts.Instruments;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

internal static class InstrumentLabels
{
    private const int ShortIdLength = 8;

    public static string KeyOf(Guid instrumentId) => $"instrument:{instrumentId:N}";

    public static string LabelOf(IReadOnlyList<InstrumentDto> catalog, Guid instrumentId)
    {
        var instrument = catalog.FirstOrDefault(each => each.Id == instrumentId);
        if (instrument is null)
            return instrumentId.ToString("N")[..ShortIdLength];

        return instrument.Label;
    }
}
