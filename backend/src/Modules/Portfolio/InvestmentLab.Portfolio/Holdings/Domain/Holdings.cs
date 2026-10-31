using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record VirtualLot(TransactionId OpenedBy, DateOnly OpenedOn, Quantity Quantity, decimal UnitCost)
{
    public decimal Cost => Quantity.Value * UnitCost;
}
