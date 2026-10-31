namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class OpenLot(
    Guid buyTransactionId,
    DateOnly acquiredOn,
    int sequence,
    Guid instrumentId,
    decimal quantity,
    decimal cost,
    decimal fxRate,
    decimal gross = 0m,

    Guid accountId = default)
{
    public Guid AccountId { get; } = accountId;
    public Guid BuyTransactionId { get; } = buyTransactionId;
    public DateOnly AcquiredOn { get; } = acquiredOn;
    public int Sequence { get; } = sequence;
    public Guid InstrumentId { get; } = instrumentId;
    public decimal Quantity { get; private set; } = quantity;
    public decimal Cost { get; private set; } = cost;
    public decimal FxRate { get; } = fxRate;
    public decimal Gross { get; private set; } = gross;

    public void Consume(decimal quantity, decimal cost, decimal gross)
    {
        Quantity -= quantity;
        Cost -= cost;
        Gross -= gross;
    }

    public OpenLotSnapshot Snapshot() => new(BuyTransactionId, AcquiredOn, InstrumentId, Quantity, Cost, FxRate, AccountId);
}
