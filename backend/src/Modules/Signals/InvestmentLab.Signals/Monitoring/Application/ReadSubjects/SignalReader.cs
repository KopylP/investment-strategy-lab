using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class SignalReader(InstrumentSubjectReader instrument, PositionSubjectReader positions, PortfolioSubjectReader portfolio)
{
    public Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken) => signal.Target.Kind switch
    {
        SignalTargetKind.Instrument => instrument.ReadAsync(signal, cancellationToken),
        SignalTargetKind.EachPosition => positions.ReadAsync(signal, cancellationToken),
        _ => portfolio.ReadAsync(signal, cancellationToken),
    };
}
