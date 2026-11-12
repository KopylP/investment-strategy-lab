using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record FillDto(
    int Number,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    DateOnly ExecutedOn,
    string? Note,
    Guid? TransactionId,
    bool Linked,
    decimal Amount)
{
    public static FillDto From(Fill fill) => new(
        Number: fill.Number,
        Quantity: fill.Quantity,
        Price: fill.Price,
        Fee: fill.Fee,
        ExecutedOn: fill.ExecutedOn,
        Note: fill.Note,
        TransactionId: fill.TransactionId,
        Linked: fill.Linked,
        Amount: fill.Amount);
}
