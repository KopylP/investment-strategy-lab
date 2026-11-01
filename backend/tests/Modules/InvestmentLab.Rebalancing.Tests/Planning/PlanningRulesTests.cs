using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class PlanningRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 1);

    private sealed record Account(IReadOnlyList<IPlanningSubPortfolio> SubPortfolios) : IPlanningAccount
    {
        public Guid Id { get; } = Guid.NewGuid();
        public UserId OwnerId { get; } = UserId.Of(Guid.NewGuid());
        public string Name => "Main";
        public string BaseCurrency => "EUR";
        public string ContributionPolicy => "none";
        public decimal DriftBand => 0m;
    }

    private sealed record SubPortfolio(bool IsActive, Guid? StrategyId, decimal? MaxPositionWeight = null, decimal? MaxMonthlyTurnover = null) : IPlanningSubPortfolio
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Sub";
        public decimal TargetShare => 1m;
        public int? PinnedStrategyVersion => 3;
        public Guid? CashInstrumentId => null;
    }

    private sealed record HeldSubPortfolio(decimal Cash, params IHeldPosition[] Held) : IHeldSubPortfolio
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IReadOnlyList<IHeldPosition> Positions => Held;
    }

    private sealed record HeldPosition(Guid InstrumentId) : IHeldPosition
    {
        public decimal Quantity => 1m;
        public decimal AverageCost => 1m;
        public IReadOnlyList<IHeldLot> Lots => [];
    }

    private sealed record Quote(string Currency, decimal Close = 10m, bool IsStale = false) : IQuotedPrice
    {
        public Guid InstrumentId { get; } = Guid.NewGuid();
        public DateOnly Date => Today;
    }

    private sealed record Foreign(string Currency, decimal Amount) : IForeignCash;

    private static DeclaredReport Declared(string code, string name, params string[] signalKinds) => new(
        Code: code,
        Name: name,
        Description: "",
        Mode: "riskReview",
        Kind: ReportKind.RiskReview,
        SubPortfolio: new SubPortfolioReport(Guid.NewGuid(), name, "Strategy", Cadence.Off),
        SignalKinds: signalKinds);

    private sealed record Price(bool IsStale) : IInstrumentPrice
    {
        public decimal Close => 10m;
        public DateOnly Date => Today;
    }

    [Fact]
    public void Only_an_active_sub_portfolio_with_a_strategy_is_planned()
    {
        var planned = new SubPortfolio(IsActive: true, Guid.NewGuid());
        var retired = new SubPortfolio(IsActive: false, Guid.NewGuid());
        var withoutStrategy = new SubPortfolio(IsActive: true, null);

        PlannedSubPortfolios.Of(new Account([planned, retired, withoutStrategy])).ShouldBe([planned]);
    }

    [Fact]
    public void A_strategy_is_asked_in_the_account_currency_and_never_sees_negative_cash()
    {
        var sub = new SubPortfolio(IsActive: true, Guid.NewGuid());

        var ask = StrategyAsk.Of(new Account([sub]), sub, new HeldSubPortfolio(-5m), 100m, Today);

        (ask.StrategyId, ask.Version, ask.Currency, ask.Cash, ask.Contribution).ShouldBe((sub.StrategyId!.Value, 3, "EUR", 0m, 100m));
    }

    [Fact]
    public void A_sub_portfolio_with_no_holdings_is_asked_with_no_cash_and_no_positions()
    {
        var sub = new SubPortfolio(IsActive: true, Guid.NewGuid());

        var ask = StrategyAsk.Of(new Account([sub]), sub, null, 0m, Today);

        ask.Cash.ShouldBe(0m);
        ask.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void A_missing_or_stale_price_is_warned_about_once_and_a_fresh_one_is_not()
    {
        var (missing, stale, fresh) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var prices = new Dictionary<Guid, IInstrumentPrice> { [stale] = new Price(IsStale: true), [fresh] = new Price(IsStale: false) };
        var labels = new Dictionary<Guid, string> { [missing] = "AAA", [stale] = "BBB" };

        var warnings = PriceWarnings.For([missing, missing, stale, fresh], prices, labels).ToList();

        warnings.Count.ShouldBe(2);
        warnings[0].ShouldContain("report.warning.no_quotes");
        warnings[0].ShouldContain("AAA");
        warnings[1].ShouldContain("report.warning.stale_quote");
        warnings[1].ShouldContain("BBB");
    }

    [Fact]
    public void A_decision_wants_its_targets_and_its_cash_instrument()
    {
        var (target, cash) = (Guid.NewGuid(), Guid.NewGuid());
        var targets = new StrategyTargets(true, Percentage.Of(1m), [new TargetWeight(target, 1m)], []);

        var decision = new StrategyDecision(new StrategyInfo(Guid.NewGuid(), 1, "code", "1"), targets, null!, cash);

        decision.WantedInstruments.ShouldBe([target, cash]);
    }

    [Fact]
    public void A_decision_without_a_cash_instrument_wants_only_its_targets()
    {
        var target = Guid.NewGuid();
        var targets = new StrategyTargets(true, Percentage.Of(1m), [new TargetWeight(target, 1m)], []);

        var decision = new StrategyDecision(new StrategyInfo(Guid.NewGuid(), 1, "code", "1"), targets, null!, null);

        decision.WantedInstruments.ShouldBe([target]);
    }

    [Fact]
    public void A_strategy_is_asked_with_the_positions_it_holds()
    {
        var sub = new SubPortfolio(IsActive: true, Guid.NewGuid());
        var position = new HeldPosition(Guid.NewGuid());

        var ask = StrategyAsk.Of(new Account([sub]), sub, new HeldSubPortfolio(1m, position), 0m, Today);

        ask.Positions.ShouldBe([position]);
    }

    [Fact]
    public void Limits_of_a_sub_portfolio_are_only_those_it_sets()
    {
        var limited = GuardrailLimits.Of(new SubPortfolio(IsActive: true, null, MaxPositionWeight: 0.2m));

        limited.MaxPositionWeight.ShouldBe(Percentage.Of(0.2m));
        limited.MaxMonthlyTurnover.ShouldBeNull();
    }

    [Theory]
    [InlineData(1.5, 1)]
    [InlineData(-0.5, 0)]
    [InlineData(0.4, 0.4)]
    public void What_a_strategy_invests_is_kept_between_nothing_and_everything(double given, double kept)
    {
        var targets = StrategyTargets.Of(true, (decimal)given, [], []);

        targets.InvestFraction.ShouldBe(Percentage.Of((decimal)kept));
    }

    [Fact]
    public void A_price_in_the_account_currency_stays_as_it_is_whatever_the_case_of_its_code()
    {
        var quote = new Quote("eur");

        var converted = PriceConversion.Of([quote], new Dictionary<string, decimal?>(), "EUR");

        converted.Prices[quote.InstrumentId].Close.ShouldBe(10m);
        converted.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void A_foreign_price_is_converted_and_warned_about_once_per_currency()
    {
        Quote[] quotes = [new Quote("USD"), new Quote("USD", Close: 20m)];
        var rates = new Dictionary<string, decimal?> { ["USD"] = 0.5m };

        var converted = PriceConversion.Of(quotes, rates, "EUR");

        converted.Prices[quotes[0].InstrumentId].Close.ShouldBe(5m);
        converted.Prices[quotes[1].InstrumentId].Close.ShouldBe(10m);
        converted.Warnings.ShouldHaveSingleItem().ShouldContain("report.warning.fx_converted");
    }

    [Fact]
    public void A_price_without_a_rate_is_left_out_and_warned_about()
    {
        var quote = new Quote("USD");
        var rates = new Dictionary<string, decimal?> { ["USD"] = null };

        var converted = PriceConversion.Of([quote], rates, "EUR");

        converted.Prices.ShouldBeEmpty();
        converted.Warnings.ShouldHaveSingleItem().ShouldContain("report.warning.fx_missing");
    }

    [Fact]
    public void Foreign_cash_is_added_at_its_rate_rounded_to_cents_and_cash_without_a_rate_counts_as_nothing()
    {
        IForeignCash[] foreign = [new Foreign("USD", 10.545m), new Foreign("GBP", 100m)];
        var rates = new Dictionary<string, decimal?> { ["USD"] = 1m, ["GBP"] = null };

        AccountCash.Of(1m, foreign, rates).ShouldBe(11.54m);
    }

    [Fact]
    public void Reports_of_one_code_become_one_report_named_by_the_first_declaration_with_each_signal_kind_once()
    {
        var reports = AccountReport.AllOf([Declared("risk", "First", "a", "b"), Declared("risk", "Second", "b", "c"), Declared("other", "Other")], null);

        reports.Count.ShouldBe(2);
        reports[0].Name.ShouldBe("First");
        reports[0].SubPortfolios.Count.ShouldBe(2);
        reports[0].SignalKinds.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void A_report_takes_the_own_schedule_of_the_account()
    {
        var settings = PlanSettings.DefaultFor(Guid.NewGuid(), UserId.Of(Guid.NewGuid()));
        settings.Schedule("risk", Cadence.Off, DateTimeOffset.UnixEpoch);

        var report = AccountReport.AllOf([Declared("risk", "Risk")], settings).ShouldHaveSingleItem();

        report.Own.ShouldBe(settings.ScheduleOf("risk"));
        report.Own.ShouldNotBeNull();
    }
}
