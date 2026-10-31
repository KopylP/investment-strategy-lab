namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class FifoLotMatcher : ILotMatcher
{
    public string Name => "FIFO";

    public IReadOnlyList<LotMatch> Take(List<OpenLot> lots, decimal quantity)
    {
        var matches = new List<LotMatch>();
        var left = quantity;

        foreach (var lot in lots.OrderBy(lot => lot.AcquiredOn).ThenBy(lot => lot.Sequence).ToList())
        {
            if (left <= 0m)
                break;

            var taken = Math.Min(lot.Quantity, left);
            var cost = lot.Quantity == taken ? lot.Cost : decimal.Round(lot.Cost * taken / lot.Quantity, 2, MidpointRounding.AwayFromZero);
            var gross = lot.Quantity == taken ? lot.Gross : lot.Gross * taken / lot.Quantity;
            matches.Add(new LotMatch(lot.BuyTransactionId, lot.AcquiredOn, taken, cost, Gross: gross, FxRate: lot.FxRate));
            lot.Consume(taken, cost, gross);

            if (lot.Quantity == 0m)
                lots.Remove(lot);

            left -= taken;
        }

        return matches;
    }
}
