using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Signals.Monitoring.Application.ChangeSignal;
using InvestmentLab.Signals.Monitoring.Application.CheckSignal;
using InvestmentLab.Signals.Monitoring.Application.CreateSignal;
using InvestmentLab.Signals.Monitoring.Application.DeleteSignal;
using InvestmentLab.Signals.Monitoring.Application.GetSignals;
using InvestmentLab.Signals.Monitoring.Application.GetSignalVocabulary;
using ModelContextProtocol.Server;

namespace InvestmentLab.Signals.Monitoring.Api;

[McpServerToolType]
internal static class SignalTools
{
    [McpServerTool(Name = "get_signal_vocabulary", ReadOnly = true)]
    [Description("Allowed values for a signal's target, condition metric and direction, and schedule frequency and weekday.")]
    public static SignalVocabularyDto GetSignalVocabulary() => GetSignalVocabularyHandler.Handle();

    [McpServerTool(Name = "get_signals", ReadOnly = true)]
    [Description("The user's signals with their conditions, schedules and recent occurrences.")]
    public static async Task<SignalsOverviewDto> GetSignals(GetSignalsHandler handler, CancellationToken cancellationToken)
    {
        var overview = await handler.Handle(cancellationToken);

        return overview.ValueOrThrow();
    }

    [McpServerTool(Name = "create_signal")]
    [Description("Creates a signal. Values for Target, Condition.Metric, Condition.Direction and Schedule come from get_signal_vocabulary. Strategy rules can use the signals that list_own_signals marks as market signals.")]
    public static async Task<SignalCreatedDto> CreateSignal(
        CreateSignalCommand signal,
        CreateSignalHandler handler,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var created = await handler.Handle(user.RequiredId, signal, cancellationToken);

        return created.ValueOrThrow();
    }

    [McpServerTool(Name = "change_signal")]
    [Description("Changes a signal's name, condition, schedule or whether it is on.")]
    public static async Task<string> ChangeSignal(
        Guid signalId,
        ChangeSignalCommand change,
        ChangeSignalHandler handler,
        CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(signalId, change, cancellationToken);
        changed.ThrowIfFailed();

        return ToolReplies.Done;
    }

    [McpServerTool(Name = "check_signal")]
    [Description("Checks a signal now instead of waiting for its schedule.")]
    public static async Task<string> CheckSignal(Guid signalId, CheckSignalHandler handler, CancellationToken cancellationToken)
    {
        var checkedSignal = await handler.Handle(signalId, cancellationToken);
        checkedSignal.ThrowIfFailed();

        return ToolReplies.Done;
    }

    [McpServerTool(Name = "delete_signal", Destructive = true)]
    [Description("Deletes a signal. Strategy rules that use it stop working until changed.")]
    public static async Task<string> DeleteSignal(Guid signalId, DeleteSignalHandler handler, CancellationToken cancellationToken)
    {
        var deleted = await handler.Handle(signalId, cancellationToken);
        deleted.ThrowIfFailed();

        return ToolReplies.Done;
    }
}
