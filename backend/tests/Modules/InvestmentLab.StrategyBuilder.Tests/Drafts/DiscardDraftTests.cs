using InvestmentLab.StrategyBuilder.Drafts.Application.DiscardDraft;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DiscardDraftTests
{
    [Fact]
    public async Task Removing_a_draft_from_a_published_version_only_removes_the_draft()
    {
        var published = new PublishedOrigin(Guid.NewGuid(), 3, "Published");
        var draft = StrategyDraft.Start(UserId.New(), "Draft", Setup, MomentumEntry(), [], published, Now).Value;
        var drafts = new Drafts(draft);
        var cleanups = new Cleanups();
        var result = await new DiscardDraftHandler(drafts, cleanups, TimeProvider.System).Handle(draft.Id, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        drafts.Current.ShouldBeNull();
        drafts.Saves.ShouldBe(1);
        cleanups.Items.ShouldBeEmpty();
        draft.Origin.ShouldBe(published);
    }

    [Fact]
    public async Task Only_the_separate_trial_strategy_is_scheduled_for_cleanup()
    {
        var published = new PublishedOrigin(Guid.NewGuid(), 2, "Published");
        var draft = StrategyDraft.Start(UserId.New(), "Draft", Setup, MomentumEntry(), [], published, Now).Value;
        var trialId = Guid.NewGuid();
        draft.RecordTrial(trialId, 1, "test", null, Now);
        var drafts = new Drafts(draft);
        var cleanups = new Cleanups();
        var handler = new DiscardDraftHandler(drafts, cleanups, TimeProvider.System);
        (await handler.Handle(draft.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        cleanups.Items.ShouldHaveSingleItem().TrialStrategyId.ShouldBe(trialId);
        cleanups.Items.ShouldNotContain(cleanup => cleanup.TrialStrategyId == published.StrategyId);
        (await handler.Handle(draft.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        cleanups.Items.Count.ShouldBe(1);
    }

    private sealed class Drafts(StrategyDraft draft) : IDraftRepository
    {
        public StrategyDraft? Current { get; private set; } = draft;
        public int Saves { get; private set; }
        public Task<StrategyDraft?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(Current?.Id == id ? Current : null);
        public Task<StrategyDraft?> FindByOriginAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyDraft>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<StrategyDraft>> ReferencingSignalAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public void Add(StrategyDraft value) => throw new NotSupportedException();
        public void Remove(StrategyDraft value) { value.ShouldBe(Current); Current = null; }
        public Task SaveChangesAsync(CancellationToken ct) { Saves++; return Task.CompletedTask; }
    }
    private sealed class Cleanups : ITrialCleanupRepository
    {
        public List<TrialCleanup> Items { get; } = [];
        public Task<TrialCleanup?> FindAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> DueAsync(DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
        public void Add(TrialCleanup value) => Items.Add(value);
        public void Remove(TrialCleanup value) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
