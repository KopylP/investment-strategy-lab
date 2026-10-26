using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

/// <summary>
/// The signed-in person's taxed accounts with their tax events, as the yearly assessment computed them (Assessments
/// does the catching up and the locking; this port keeps returns away from its application layer).
/// </summary>
public interface ITaxpayerHistory
{
    Task<TaxpayerHistoryDto> LoadAsync(CancellationToken cancellationToken);
}

/// <param name="Events">The account's own events (lots never cross accounts).</param>
public sealed record TaxpayerAccountHistory(Guid Id, string Name, string Broker, ITaxPolicy Policy, IReadOnlyList<TaxEvent> Events);

/// <param name="Losses">Losses from before the journal the person entered (theirs, not an account's).</param>
/// <param name="Incomplete">What the figures miss (NBP rates, purchases of sold units): a return is not filled from them.</param>
public sealed record TaxpayerHistoryDto(
    IReadOnlyList<TaxpayerAccountHistory> Accounts, IReadOnlyList<LossYear> Losses, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete);
