namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record Fill(int Number, decimal Quantity, decimal Price, decimal Fee, DateOnly ExecutedOn, string? Note, Guid? TransactionId, bool Linked = false)
{
    public decimal Amount => decimal.Round(Quantity * Price, 2);

    public static Result<Fill> TryOf(int number, decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note)
    {
        if (quantity <= 0m)
            return ReportErrors.FillQuantityNotPositive();

        if (price <= 0m)
            return ReportErrors.FillPriceNotPositive();

        if (fee < 0m)
            return ReportErrors.FillFeeNegative();

        return new Fill(number, quantity, price, fee, executedOn, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), null);
    }
}
