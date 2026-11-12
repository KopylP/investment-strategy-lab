namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class LossPool(LossYear loss)
{
    public LossYear Loss { get; } = loss;
    public decimal Remaining { get; private set; } = Math.Max(0m, loss.Amount - loss.AlreadyUsed);

    public static IReadOnlyList<CarryforwardUse> UseAgainst(IEnumerable<LossPool> pools, int year, decimal gain, ILossCarryforwardRule rule)
    {
        var uses = new List<CarryforwardUse>();
        var left = Math.Max(0m, gain);

        foreach (var pool in pools.Where(pool => pool.Loss.Year < year).OrderBy(pool => pool.Loss.Year))
        {
            if (left <= 0m)
                break;

            var usable = Math.Min(Math.Min(rule.UsableIn(pool.Loss, year), pool.Remaining), left);
            if (usable <= 0m)
                continue;

            pool.Remaining -= usable;
            uses.Add(new CarryforwardUse(pool.Loss.Year, usable));
            left -= usable;
        }

        return uses;
    }
}
