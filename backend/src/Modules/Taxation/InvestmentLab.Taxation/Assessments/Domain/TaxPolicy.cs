namespace InvestmentLab.Taxation.Assessments.Domain;

/// <summary>Which units a sale takes from the open lots.</summary>
public interface ILotMatcher
{
    string Name { get; }

    /// <summary>Takes <paramref name="quantity"/> units from <paramref name="lots"/> (changing them) and says from which lots.</summary>
    IReadOnlyList<LotMatch> Take(List<OpenLot> lots, decimal quantity);
}

/// <summary>Tax rate on the year's taxable base.</summary>
public interface ITaxRateSchedule
{
    Percentage Rate { get; }
}

/// <summary>How much of an earlier year's loss may reduce a later year's gain.</summary>
public interface ILossCarryforwardRule
{
    /// <summary>The most of <paramref name="loss"/> that may be used in <paramref name="year"/>; 0 when the loss has expired.</summary>
    decimal UsableIn(LossYear loss, int year);
}

/// <summary>Rounding of the tax base and of the tax.</summary>
public interface IRoundingRule
{
    decimal Base(decimal amount);
    decimal Tax(decimal amount);
}

/// <summary>
/// A tax policy as a composition of rules (§6.7). Exchange rates are part of the input: every event carries the rate of
/// its currency to PLN chosen by the policy's FX rule (NBP, last business day before the transaction).
/// </summary>
public interface ITaxPolicy
{
    string Code { get; }
    int Version { get; }

    /// <summary>The country whose rules these are (ISO code); declaration forms pick their accounts by it.</summary>
    string Jurisdiction { get; }
    bool IsTaxed { get; }
    string FxRule { get; }
    ILotMatcher LotMatcher { get; }
    ITaxRateSchedule RateSchedule { get; }
    ILossCarryforwardRule LossCarryforward { get; }
    IRoundingRule Rounding { get; }
}

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
            var cost = lot.Quantity == taken ? lot.Cost : decimal.Round(lot.Cost * taken / lot.Quantity, 2);
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

public sealed class FlatRate(Percentage rate) : ITaxRateSchedule
{
    public Percentage Rate => rate;
}

/// <summary>Polish rule: a loss reduces the gains of the next five years, by at most half of it in any one year.</summary>
public sealed class Pl5Years50Percent : ILossCarryforwardRule
{
    public decimal UsableIn(LossYear loss, int year) =>
        year > loss.Year && year <= loss.Year + 5 ? decimal.Round(loss.Amount / 2m, 2) : 0m;
}

public sealed class NoCarryforward : ILossCarryforwardRule
{
    public decimal UsableIn(LossYear loss, int year) => 0m;
}

/// <summary>Polish rule: base and tax to whole złoty, half a złoty and more rounds up.</summary>
public sealed class PlWholeZloty : IRoundingRule
{
    public decimal Base(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
    public decimal Tax(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
}

/// <summary>"Podatek Belki": FIFO, NBP rate of the business day before, 19%, yearly netting, losses 5 years × 50%, whole złoty.</summary>
public sealed class BelkaTaxPolicy : ITaxPolicy
{
    public const string PolicyCode = "PL-BELKA";

    public string Code => PolicyCode;
    public string Jurisdiction => TaxPolicies.Poland;

    // 2: dividends taxed on gross with credit for the tax withheld abroad (PIT-38 part G).
    // 3: the sale's fee is a cost, not taken off the revenue (PIT-38 poz. 22/23).
    public int Version => 3;
    public bool IsTaxed => true;
    public string FxRule => "NBP, остання таблиця A перед днем угоди";
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Percentage.FromPercent(19m));
    public ILossCarryforwardRule LossCarryforward { get; } = new Pl5Years50Percent();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}

/// <summary>IKE / IKZE: gains are tracked but not taxed.</summary>
public sealed class NoTaxPolicy(string code) : ITaxPolicy
{
    public string Code => code;
    public string Jurisdiction => TaxPolicies.Poland;
    public int Version => 1;
    public bool IsTaxed => false;
    public string FxRule => "NBP, остання таблиця A перед днем угоди";
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Percentage.Zero);
    public ILossCarryforwardRule LossCarryforward { get; } = new NoCarryforward();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}

public static class TaxPolicies
{
    public const string Poland = "PL";

    public static ITaxPolicy For(string? code) => code?.ToUpperInvariant() switch
    {
        "PL-IKE" or "PL-IKZE" => new NoTaxPolicy(code.ToUpperInvariant()),
        _ => new BelkaTaxPolicy(),
    };
}
