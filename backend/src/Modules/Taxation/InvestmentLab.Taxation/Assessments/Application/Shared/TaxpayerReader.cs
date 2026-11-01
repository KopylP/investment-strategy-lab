using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxpayerReader(ITaxpayerAccounts accounts, TaxAccountSync sync, IAssessmentRepository assessments)
{
    public async Task<Taxpayer> ReadAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var owned = await accounts.OfOwnerAsync(ownerId, cancellationToken);
        var setups = owned.OrderBy(setup => setup.Name).ToList();
        var known = new List<TaxAccount>();

        foreach (var setup in setups)
        {
            if (await sync.EnsureAccountAsync(setup.Id, cancellationToken) is { } account)
                known.Add(account);
        }

        var identities = setups.ToDictionary(setup => setup.Id, setup => new TaxpayerAccountIdentity(setup.Name, setup.Broker));

        return new Taxpayer(ownerId, known, identities);
    }

    public async Task<IReadOnlyList<LossYear>> LossesAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var losses = await assessments.LossesAsync(ownerId, cancellationToken);

        return [.. losses.Select(loss => loss.ToLossYear())];
    }
}
