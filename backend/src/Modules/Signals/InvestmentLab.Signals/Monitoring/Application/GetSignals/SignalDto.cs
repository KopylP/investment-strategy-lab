using InvestmentLab.Signals.Monitoring.Application.Shared;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.GetSignals;

public sealed record SignalDto(
    Guid Id,
    string Name,
    string Target,
    Guid? AccountId,
    Guid? SubPortfolioId,
    Guid? InstrumentId,
    string? GroupName,
    SignalConditionDto Condition,
    SignalScheduleDto Schedule,
    bool Enabled,
    bool BuiltIn,
    DateTimeOffset? LastCheckedAt,
    int ActiveCount)
{
    public static SignalDto Of(Signal signal, int activeCount) => new(
        Id: signal.Id,
        Name: signal.Name,
        Target: signal.Target.Kind.ToString(),
        AccountId: signal.Target.AccountId,
        SubPortfolioId: signal.Target.SubPortfolioId,
        InstrumentId: signal.Target.InstrumentId,
        GroupName: signal.Target.GroupName,
        Condition: SignalConditionDto.Of(signal.Condition),
        Schedule: SignalScheduleDto.Of(signal.Schedule),
        Enabled: signal.Enabled,
        BuiltIn: signal.IsBuiltIn,
        LastCheckedAt: signal.LastCheckedAt,
        ActiveCount: activeCount);
}
