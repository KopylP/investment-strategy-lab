namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record CashMovementDetails(CashMovementKind Kind, Money Amount, IReadOnlyList<CashAllocation> Allocations, Guid? InstrumentId, Money? Settlement = null)
    : TransactionDetails
{
    public bool IsInflow => Kind is CashMovementKind.Contribution or CashMovementKind.Dividend;
}
