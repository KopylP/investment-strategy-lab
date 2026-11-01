using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Ledger.Infrastructure;

internal sealed class TaxpayerAccounts(IAccountSetupQueries accounts) : ITaxpayerAccounts
{
    public async Task<ITaxpayerAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.GetAccountSetup(accountId, cancellationToken);

        return setup is null ? null : new TaxpayerAccount(setup);
    }

    public async Task<IReadOnlyList<ITaxpayerAccount>> OfOwnerAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var all = await accounts.GetAccountSetups(cancellationToken);
        var owned = all.Where(setup => setup.OwnerId == ownerId.Value);

        return [.. owned.Select(setup => new TaxpayerAccount(setup))];
    }

    private sealed class TaxpayerAccount(AccountSetupDto dto) : ITaxpayerAccount
    {
        private readonly AccountSetupDto _dto = dto;

        public Guid Id => _dto.Id;
        public UserId OwnerId => UserId.Of(_dto.OwnerId);
        public string Name => _dto.Name;
        public string Broker => _dto.Broker;
        public string Currency => _dto.BaseCurrency;
        public string PolicyCode => _dto.TaxPolicyCode;
    }
}
