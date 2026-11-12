using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;

public sealed class InstrumentDescriptionsJob(DescribeInstrumentsHandler describe, ILogger<InstrumentDescriptionsJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } =
        new DailySchedule(new TimeOnly(23, 15), TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"), WeekdaysOnly: false);

    public static bool RunAtStartup => true;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var described = await describe.Handle(null, cancellationToken);
        logger.LogInformation("Instrument descriptions: {Described} answered by the quote provider", described);
    }
}
