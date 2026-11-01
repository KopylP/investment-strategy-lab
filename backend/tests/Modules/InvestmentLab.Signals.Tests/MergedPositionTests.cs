using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class MergedPositionTests
{
    private static readonly Guid Etf = Guid.NewGuid();

    private sealed record Part(Guid InstrumentId, decimal Quantity, decimal CostBasis, DateOnly FirstOpenedOn) : IOpenPosition;

    [Fact]
    public void Parts_of_one_instrument_merge_into_their_average_cost_and_earliest_purchase()
    {
        Part[] parts = [new Part(Etf, 1m, 10m, new(2026, 2, 1)), new Part(Etf, 3m, 50m, new(2026, 1, 1))];

        var merged = MergedPosition.AllOf(parts).ShouldHaveSingleItem();

        merged.ShouldBe(new MergedPosition(Etf, 15m, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void A_closed_part_counts_neither_for_the_cost_nor_for_the_date()
    {
        Part[] parts = [new Part(Etf, 0m, 99m, new(2020, 1, 1)), new Part(Etf, 2m, 20m, new(2026, 1, 1))];

        MergedPosition.AllOf(parts).ShouldHaveSingleItem().ShouldBe(new MergedPosition(Etf, 10m, new DateOnly(2026, 1, 1)));
    }
}
