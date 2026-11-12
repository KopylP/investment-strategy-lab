using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchFillDto(
    Guid InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal ReferencePrice,
    decimal Commission,
    decimal SpreadCost,
    decimal SlippageCost,
    DateOnly DecisionDate)
{
    public static ResearchFillDto From(ReplayFill replayFill) => new(
        InstrumentId: replayFill.InstrumentId,
        Quantity: replayFill.Quantity,
        Price: replayFill.Price,
        ReferencePrice: replayFill.ReferencePrice,
        Commission: replayFill.Commission,
        SpreadCost: replayFill.SpreadCost,
        SlippageCost: replayFill.SlippageCost,
        DecisionDate: replayFill.DecisionDate);
}
