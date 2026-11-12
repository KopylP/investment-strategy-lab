using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYear;
using InvestmentLab.Taxation.Assessments.Application.RefreshTaxAssessments;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;
using InvestmentLab.Taxation.Ledger.Infrastructure;

namespace InvestmentLab.Taxation.Tests;

public sealed class GetTaxYearTests
{
    private static readonly Guid AccountId = Guid.NewGuid(), Etf = Guid.NewGuid(), Sub = Guid.NewGuid();
    private static readonly UserId Owner = UserId.Of(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2020, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class SignedIn : ICurrentUser
    {
        public UserId? Id => Owner;
    }

    private sealed class OneAccount : IAccountSetupQueries
    {
        private readonly AccountSetupDto _setup = new(AccountId, Owner.Value, "Main", "Broker", "PLN", BelkaTaxPolicy.PolicyCode, "none", 0m, []);

        public Task<AccountSetupDto?> GetAccountSetup(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<AccountSetupDto?>(_setup);

        public Task<IReadOnlyList<AccountSetupDto>> GetAccountSetups(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AccountSetupDto>>([_setup]);
    }

    private sealed class EmptyJournal : IJournalQueries
    {
        public Task<JournalSnapshotDto?> GetJournal(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<JournalSnapshotDto?>(new JournalSnapshotDto(accountId, Owner.Value, "PLN", [], []));
    }

    private sealed class NoInstruments : IInstrumentCatalog
    {
        public Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InstrumentDto>>([]);
    }

    private sealed class NoInstrumentKeys : IInstrumentKeys
    {
        public Task<IReadOnlyDictionary<Guid, InstrumentKey>> AllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, InstrumentKey>>(new Dictionary<Guid, InstrumentKey>());
    }

    private sealed class NoStatements : IBrokerStatementRepository
    {
        public Task<IReadOnlyList<BrokerStatement>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BrokerStatement>>([]);

        public void Add(BrokerStatement statement) => throw new NotSupportedException();

        public void Remove(BrokerStatement statement) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class BuyAndDividendLedger : ITaxLedgerRepository
    {
        private readonly TaxAccount _account = TaxAccount.Of(AccountId, Owner, "PLN", BelkaTaxPolicy.PolicyCode);

        private readonly TaxLedgerEntry _buy = TaxLedgerEntry.Trade(
            Guid.NewGuid(), AccountId, Owner, new DateOnly(2020, 2, 3), Now, isBuy: true, Etf, 2m, 100m, 0m, "PLN", [new EntryAllocation(Sub, 2m)]);

        private readonly TaxLedgerEntry _dividend = TaxLedgerEntry.Dividend(
            Guid.NewGuid(), AccountId, Owner, new DateOnly(2020, 3, 3), Now, Etf, 10m, "PLN", [new EntryAllocation(Sub, 10m)]);

        public Task<TaxAccount?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<TaxAccount?>(_account);

        public Task<IReadOnlyList<TaxLedgerEntry>> EntriesAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TaxLedgerEntry>>([_buy, _dividend]);

        public Task<TaxLedgerEntry?> FindEntryAsync(Guid transactionId, CancellationToken cancellationToken) => Task.FromResult<TaxLedgerEntry?>(_buy);

        public void AddAccount(TaxAccount account) => throw new NotSupportedException();

        public void AddEntry(TaxLedgerEntry entry) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryAssessments : IAssessmentRepository
    {
        private readonly List<TaxAssessment> _assessments = [];

        public int Saves { get; private set; }

        public Task<IReadOnlyList<TaxAssessment>> ForOwnerAsync(UserId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TaxAssessment>>(_assessments);

        public Task<IReadOnlyList<LossCarryforward>> LossesAsync(UserId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LossCarryforward>>([]);

        public void Add(TaxAssessment assessment) => _assessments.Add(assessment);

        public void AddLoss(LossCarryforward loss) => throw new NotSupportedException();

        public void RemoveLoss(LossCarryforward loss) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;

            return Task.CompletedTask;
        }

        public Task<T> SerializedAsync<T>(UserId ownerId, Func<Task<T>> work, CancellationToken cancellationToken) => work();
    }

    [Fact]
    public async Task Open_lots_belong_to_the_year_of_the_injected_clock()
    {
        var clock = new FixedClock(Now);
        var repository = new InMemoryAssessments();
        var ledger = new BuyAndDividendLedger();
        var accounts = new TaxpayerAccounts(new OneAccount());
        var taxpayers = new TaxpayerReader(accounts, new TaxAccountSync(ledger, accounts, new TaxJournals(new EmptyJournal())), repository);
        var taxEvents = new TaxEventReader(ledger, new NoStatements(), null!, new NoInstrumentKeys());
        var recalculator = new TaxRecalculator(new OneAccount(), taxpayers, taxEvents, repository, clock);
        await new RefreshTaxAssessmentsHandler(recalculator, new SignedIn()).Handle(TestContext.Current.CancellationToken);
        var query = new GetTaxYearHandler(repository, new NoInstruments(), recalculator, new SignedIn(), clock);

        var saves = repository.Saves;
        var details = await query.Handle(Now.Year, TestContext.Current.CancellationToken);

        details.Value.OpenLots.ShouldNotBeEmpty();
        repository.Saves.ShouldBe(saves);
    }
}
