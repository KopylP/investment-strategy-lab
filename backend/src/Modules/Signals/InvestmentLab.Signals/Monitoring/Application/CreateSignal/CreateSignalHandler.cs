using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Signals.Monitoring.Application.Shared;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.CreateSignal;

public sealed record CreateSignalCommand(
    string Name,
    string Target,
    Guid? AccountId,
    Guid? SubPortfolioId,
    Guid? InstrumentId,
    SignalConditionDto Condition,
    SignalScheduleDto Schedule,
    string? GroupName = null);

public sealed record SignalCreatedDto(Guid Id);

public sealed class CreateSignalHandler(ISignalRepository signals, IAccountSetupQueries accounts, IInstrumentCatalog instruments)
{
    public async Task<Result<SignalCreatedDto>> Handle(UserId ownerId, CreateSignalCommand command, CancellationToken cancellationToken)
    {
        var target = await ResolveTargetAsync(command, cancellationToken);
        if (target.IsFailure)
            return target.Error;

        var condition = command.Condition.ToCondition();
        if (condition.IsFailure)
            return condition.Error;

        var schedule = command.Schedule.ToCadence();
        if (schedule.IsFailure)
            return schedule.Error;

        var signal = Signal.Create(ownerId, command.Name, target.Value, condition.Value, schedule.Value);
        if (signal.IsFailure)
            return signal.Error;

        signals.Add(signal.Value);
        await signals.SaveChangesAsync(cancellationToken);

        return new SignalCreatedDto(signal.Value.Id);
    }

    private async Task<Result<SignalTarget>> ResolveTargetAsync(CreateSignalCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SignalTargetKind>(command.Target, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return SignalErrors.UnknownTarget();

        return kind switch
        {
            SignalTargetKind.Instrument => await InstrumentTargetAsync(command.InstrumentId, cancellationToken),
            SignalTargetKind.InstrumentGroup => await InstrumentGroupTargetAsync(command.GroupName, cancellationToken),
            SignalTargetKind.SubPortfolio => await SubPortfolioTargetAsync(command.AccountId, command.SubPortfolioId, cancellationToken),
            SignalTargetKind.EachPosition => await AccountTargetAsync(command.AccountId, SignalTarget.EachPosition, cancellationToken),
            _ => await AccountTargetAsync(command.AccountId, SignalTarget.Account, cancellationToken),
        };
    }

    private async Task<Result<SignalTarget>> InstrumentTargetAsync(Guid? instrumentId, CancellationToken cancellationToken)
    {
        if (instrumentId is not { } id)
            return SignalErrors.UnknownTarget();

        var catalog = await instruments.GetInstruments(cancellationToken);
        if (catalog.All(instrument => instrument.Id != id))
            return SignalErrors.InstrumentNotFound();

        return SignalTarget.Instrument(id);
    }

    private async Task<Result<SignalTarget>> InstrumentGroupTargetAsync(string? groupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return SignalErrors.UnknownTarget();

        var catalog = await instruments.GetInstruments(cancellationToken);
        if (catalog.All(instrument => instrument.Group != groupName))
            return SignalErrors.InstrumentGroupNotFound();

        return SignalTarget.InstrumentGroup(groupName);
    }

    private async Task<Result<SignalTarget>> SubPortfolioTargetAsync(Guid? accountId, Guid? subPortfolioId, CancellationToken cancellationToken)
    {
        if (accountId is not { } account || subPortfolioId is not { } subPortfolio)
            return SignalErrors.UnknownTarget();

        var setup = await accounts.GetAccountSetup(account, cancellationToken);
        if (setup is null)
            return SignalErrors.AccountNotFound();

        if (setup.SubPortfolios.All(each => each.Id != subPortfolio))
            return SignalErrors.SubPortfolioNotFound();

        return SignalTarget.SubPortfolio(account, subPortfolio);
    }

    private async Task<Result<SignalTarget>> AccountTargetAsync(
        Guid? accountId, Func<Guid, SignalTarget> targetOf, CancellationToken cancellationToken)
    {
        if (accountId is not { } account)
            return SignalErrors.UnknownTarget();

        if (await accounts.GetAccountSetup(account, cancellationToken) is null)
            return SignalErrors.AccountNotFound();

        return targetOf(account);
    }
}
