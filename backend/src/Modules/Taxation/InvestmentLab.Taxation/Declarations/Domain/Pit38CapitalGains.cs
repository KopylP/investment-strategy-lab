using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record Pit38CapitalGains(
    decimal P20,
    decimal P21,
    decimal P22,
    decimal P23,
    decimal P26,
    decimal P27,
    decimal P28,
    decimal P29,
    decimal P30,
    decimal P31,
    decimal P33,
    decimal P34,
    decimal P35)
{
    public static Pit38CapitalGains Of(DeclarationInput input, Pit38BrokerRows rows, List<DeclarationAccount> taxed, ITaxPolicy policy)
    {
        var year = input.Year;
        var rate = policy.RateSchedule.Rate.Value;
        var row2 = rows.WithoutPit8CForm;

        var (p20, p21) = rows.Row1Of(year);
        var p22 = row2.Sum(account => account.In(year)?.Revenue ?? 0m);
        var p23 = row2.Sum(account => account.In(year)?.Costs ?? 0m);
        var (p26, p27) = (p20 + p22, p21 + p23);
        var p28 = Math.Max(0m, p26 - p27);
        var p29 = Math.Max(0m, p27 - p26);

        var gains = GainsByYear(rows, taxed);
        gains[year] = p26 - p27;

        var p30 = CarryforwardUsed(year, gains, input.EarlierLosses, policy.LossCarryforward);
        var p31 = policy.Rounding.Base(p28 - p30);
        var p33 = Pit38Amounts.Grosze(p31 * rate);
        const decimal p34 = 0m;
        var p35 = policy.Rounding.Tax(Math.Max(0m, p33 - p34));

        return new Pit38CapitalGains(p20, p21, p22, p23, p26, p27, p28, p29, p30, p31, p33, p34, p35);
    }

    private static Dictionary<int, decimal> GainsByYear(Pit38BrokerRows rows, List<DeclarationAccount> taxed)
    {
        var years = taxed.SelectMany(account => account.Years).Select(summary => summary.Year).Distinct();

        return years.ToDictionary(year => year, year => GainOf(rows, year));
    }

    private static decimal GainOf(Pit38BrokerRows rows, int year)
    {
        var (Revenue, Costs) = rows.Row1Of(year);

        return Revenue - Costs + rows.WithoutPit8CForm.Sum(account => account.In(year)?.Gain ?? 0m);
    }

    private static decimal CarryforwardUsed(
        int year,
        IReadOnlyDictionary<int,
        decimal> gains,
        IReadOnlyList<LossYear> earlierLosses,
        ILossCarryforwardRule rule)
    {
        var entered = earlierLosses.Select(loss => loss.Year).ToHashSet();
        var pools = earlierLosses.Select(loss => new LossPool(loss)).ToList();
        var first = gains.Keys.Min();
        var used = 0m;

        for (var number = first; number <= year; number++)
        {
            var gain = gains.GetValueOrDefault(number);
            used = LossPool.UseAgainst(pools, number, gain, rule).Sum(use => use.Amount);

            if (gain < 0m && !entered.Contains(number))
                pools.Add(new LossPool(new LossYear(number, -gain)));
        }

        return used;
    }
}
