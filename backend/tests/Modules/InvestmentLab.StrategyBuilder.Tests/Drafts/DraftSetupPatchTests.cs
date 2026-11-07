using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftSetupPatchTests
{
    private static readonly Guid FirstFund = Guid.NewGuid();
    private static readonly Guid SecondFund = Guid.NewGuid();
    private static readonly Guid Benchmark = Guid.NewGuid();
    private static readonly Guid Cash = Guid.NewGuid();
    private static readonly Guid VolatilityIndex = Guid.NewGuid();
    private static readonly Guid Extra = Guid.NewGuid();

    private static readonly DraftSetup Full = new(
        Funds: [FirstFund, SecondFund],
        BenchmarkId: Benchmark,
        CashId: Cash,
        VolatilityIndexId: VolatilityIndex,
        ExtraSeries: [Extra]);

    [Fact]
    public void A_patch_with_only_the_benchmark_keeps_everything_else()
    {
        var newBenchmark = Guid.NewGuid();
        var patch = new DraftSetupPatch(Funds: null, BenchmarkId: newBenchmark, CashId: null, VolatilityIndexId: null, ExtraSeries: null);

        var patched = patch.Over(Full).Value;

        patched.BenchmarkId.ShouldBe(newBenchmark);
        patched.Funds.ShouldBe([FirstFund, SecondFund]);
        patched.CashId.ShouldBe(Cash);
        patched.VolatilityIndexId.ShouldBe(VolatilityIndex);
        patched.ExtraSeries.ShouldBe([Extra]);
    }

    [Fact]
    public void A_patch_with_only_funds_keeps_a_missing_cash_fund_missing()
    {
        var withoutCash = Full with { CashId = null };
        var patch = new DraftSetupPatch(Funds: [SecondFund], BenchmarkId: null, CashId: null, VolatilityIndexId: null, ExtraSeries: null);

        var patched = patch.Over(withoutCash).Value;

        patched.Funds.ShouldBe([SecondFund]);
        patched.CashId.ShouldBeNull();
        patched.BenchmarkId.ShouldBe(Benchmark);
    }

    [Fact]
    public void An_empty_patch_leaves_the_setup_as_it_is()
    {
        var patch = new DraftSetupPatch(Funds: null, BenchmarkId: null, CashId: null, VolatilityIndexId: null, ExtraSeries: null);

        var patched = patch.Over(Full).Value;

        patched.Funds.ShouldBe(Full.Funds);
        patched.BenchmarkId.ShouldBe(Full.BenchmarkId);
        patched.CashId.ShouldBe(Full.CashId);
        patched.VolatilityIndexId.ShouldBe(Full.VolatilityIndexId);
        patched.ExtraSeries.ShouldBe(Full.ExtraSeries);
    }

    [Fact]
    public void A_patch_repeating_a_fund_is_refused()
    {
        var patch = new DraftSetupPatch(Funds: [FirstFund, FirstFund], BenchmarkId: null, CashId: null, VolatilityIndexId: null, ExtraSeries: null);

        var patched = patch.Over(Full);

        patched.IsFailure.ShouldBeTrue();
    }
}
