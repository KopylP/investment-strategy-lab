using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.GoldenTests;

public sealed class SubPortfolioRebalancerGoldenTests
{
    private const decimal Cent = 0.01m;
    private const decimal ShareTolerance = 0.000001m;
    private const decimal WeightTolerance = 0.000001m;

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();

        foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Fixtures"), "*.json", SearchOption.AllDirectories).Order())
            data.Add(Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "Fixtures"), path));

        return data;
    }

    [Fact]
    public void All_fixtures_are_present() => Fixtures().Count.ShouldBe(29);

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Orders_match_plan_py(string fixture)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture)));
        var root = json.RootElement;
        var expected = root.GetProperty("expected");

        var plan = SubPortfolioRebalancer.Plan(InputOf(root));

        var orders = expected.GetProperty("orders").EnumerateArray().ToList();
        plan.Orders.Select(Describe).ShouldBe(orders.Select(order => $"{order.GetProperty("kind").GetString()} {order.GetProperty("ticker").GetString()}"));

        foreach (var (actual, order) in plan.Orders.Zip(orders))
        {
            actual.Amount.ShouldBe(order.GetProperty("amount").GetDecimal(), Cent, Describe(actual));
            actual.Quantity.ShouldBe(order.GetProperty("shares").GetDecimal(), ShareTolerance, Describe(actual));
            actual.Price.ShouldBe(order.GetProperty("price").GetDecimal());
            MessageLocalizer.In(InvestmentLab.SharedKernel.Language.Ukrainian).Render(actual.Reason).ShouldBe(order.GetProperty("reason").GetString());
        }

        plan.TotalBefore.ShouldBe(expected.GetProperty("totalBefore").GetDecimal(), Cent);
        plan.TotalAfter.ShouldBe(expected.GetProperty("totalAfter").GetDecimal(), Cent);
        plan.ParkedBefore.ShouldBe(expected.GetProperty("parkedBefore").GetDecimal(), Cent);
        plan.ParkedAfter.ShouldBe(expected.GetProperty("parkedAfter").GetDecimal(), Cent);

        var rows = expected.GetProperty("allocation").EnumerateArray().ToList();
        plan.Allocation.Count.ShouldBe(rows.Count);

        foreach (var (actual, row) in plan.Allocation.Zip(rows))
        {
            var ticker = row.GetProperty("ticker").GetString()!;
            var label = $"{row.GetProperty("role").GetString()} {ticker}";
            actual.Role.ToString().ToLowerInvariant().ShouldBe(row.GetProperty("role").GetString(), label);
            actual.InstrumentId.ShouldBe(ticker == "CASH" ? null : IdOf(ticker), label);
            actual.ValueBefore.ShouldBe(row.GetProperty("valueBefore").GetDecimal(), Cent, label);
            actual.ValueAfter.ShouldBe(row.GetProperty("valueAfter").GetDecimal(), Cent, label);
            actual.WeightBefore.ShouldBe(row.GetProperty("weightBefore").GetDecimal(), WeightTolerance, label);
            actual.WeightAfter.ShouldBe(row.GetProperty("weightAfter").GetDecimal(), WeightTolerance, label);
            var target = row.GetProperty("targetWeight");
            if (target.ValueKind == JsonValueKind.Null)
                actual.TargetWeight.ShouldBeNull(label);
            else
                actual.TargetWeight!.Value.ShouldBe(target.GetDecimal(), WeightTolerance, label);
        }

        var notes = expected.GetProperty("notes").EnumerateArray()
            .Select(note => note.GetString()!.Replace(" Вкажіть monthly_contribution у config або передайте --contribution.", ""));
        plan.Notes.Select(note => MessageLocalizer.In(InvestmentLab.SharedKernel.Language.Ukrainian).Render(note)).ShouldBe(notes);
    }

    private static SubPortfolioSituation InputOf(JsonElement root)
    {
        var decision = root.GetProperty("decision");
        var portfolio = root.GetProperty("portfolio");
        var settings = root.GetProperty("settings");
        var costBasis = portfolio.GetProperty("costBasis").EnumerateObject().ToDictionary(entry => entry.Name, entry => entry.Value.GetDecimal());

        var strategy = new StrategyTargets(
            decision.GetProperty("marketOk").GetBoolean(),
            Percentage.Of(decision.GetProperty("investFraction").GetDecimal()),
            [.. decision.GetProperty("targets").EnumerateArray().Select(target =>
                new TargetWeight(IdOf(target.GetProperty("key").GetString()!), target.GetProperty("weight").GetDecimal()))],
            [.. decision.GetProperty("scores").EnumerateArray().Select(score => new InstrumentScore(
                IdOf(score.GetProperty("key").GetString()!),
                score.GetProperty("rank") is { ValueKind: JsonValueKind.Number } rank ? rank.GetInt32() : null,
                score.GetProperty("momentum") is { ValueKind: JsonValueKind.Number } momentum ? momentum.GetDecimal() : null,
                score.GetProperty("reason").GetString()!))]);

        var positions = portfolio.GetProperty("holdings").EnumerateArray().Select(holding =>
        {
            var key = holding.GetProperty("key").GetString()!;

            return new SubPortfolioPosition(IdOf(key), Quantity.Of(holding.GetProperty("quantity").GetDecimal()), costBasis.GetValueOrDefault(key));
        }).ToList();

        var prices = root.GetProperty("prices").EnumerateObject()
            .Where(entry => entry.Value.ValueKind == JsonValueKind.Number)
            .ToDictionary(entry => IdOf(entry.Name), entry => entry.Value.GetDecimal());

        var tickers = root.GetProperty("prices").EnumerateObject().Select(entry => entry.Name)
            .Concat(positions.Count == 0 ? [] : portfolio.GetProperty("holdings").EnumerateArray().Select(holding => holding.GetProperty("key").GetString()!))
            .Distinct()
            .ToDictionary(IdOf, ticker => ticker);

        return new SubPortfolioSituation(
            strategy,
            positions,
            portfolio.GetProperty("cash").GetDecimal(),
            root.GetProperty("contribution").GetDecimal(),
            prices,
            root.GetProperty("cashInstrument").GetString() is { } cash ? IdOf(cash) : null,
            RebalancingPolicy.Of(
                settings.GetProperty("minTrade").GetDecimal(),
                Percentage.Of(settings.GetProperty("trimBand").GetDecimal()),
                settings.GetProperty("fractionalShares").GetBoolean()),
            Currency.Of(root.GetProperty("currency").GetString()!),
            tickers);
    }

    private static readonly Dictionary<Guid, string> Tickers = [];

    private static Guid IdOf(string ticker)
    {
        var id = new Guid(MD5.HashData(Encoding.UTF8.GetBytes(ticker)));

        lock (Tickers)
            Tickers[id] = ticker;

        return id;
    }

    private static string Describe(PlannedOrder order)
    {
        lock (Tickers)
            return $"{order.Kind.ToString().ToLowerInvariant()} {Tickers[order.InstrumentId]}";
    }
}
