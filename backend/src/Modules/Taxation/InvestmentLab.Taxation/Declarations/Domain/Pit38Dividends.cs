using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record Pit38Dividends(
    decimal P47,
    decimal P48,
    decimal P49,
    IReadOnlyList<DividendIncome> Foreign,
    IReadOnlyList<(DeclarationAccount Account, DividendIncome Dividend)> Polish,
    IReadOnlyList<DividendIncome> OverTreaty,
    decimal KisCredit,
    decimal KisP49,
    IReadOnlyList<DividendIncome> WithholdingUnknown)
{
    private const string PolandCode = "PL";
    private const decimal UsualTreatyRate = 0.15m;
    private const decimal Grosz = 0.01m;

    private static readonly Dictionary<string, decimal> TreatyRates = new() { ["JP"] = 0.10m, ["IT"] = 0.10m };

    public static Pit38Dividends Of(DeclarationInput input, List<DeclarationAccount> taxed, ITaxPolicy policy)
    {
        var countryOf = input.CountryOf;
        var rate = BelkaTaxPolicy.Rate.Value;
        var rounding = policy.Rounding;

        var foreign = taxed.SelectMany(account => account.Dividends).Where(dividend => countryOf(dividend.InstrumentId) != PolandCode).ToList();
        var polish = taxed.SelectMany(account => account.Dividends.Select(dividend => (Account: account, Dividend: dividend)))
            .Where(paid => countryOf(paid.Dividend.InstrumentId) == PolandCode)
            .ToList();

        var p47 = Pit38Amounts.Grosze(foreign.Sum(dividend => dividend.GrossPln) * rate);
        var creditAtTaxRate = Pit38Amounts.Grosze(foreign.Sum(dividend => Math.Min(dividend.WithheldPln, dividend.GrossPln * rate)));
        var p48 = Math.Min(p47, creditAtTaxRate);
        var p49 = rounding.Tax(Math.Max(0m, p47 - p48));

        decimal TreatyRate(DividendIncome dividend) => TreatyRateOf(countryOf(dividend.InstrumentId));

        var overTreaty = foreign.Where(dividend => IsOverTreaty(dividend, TreatyRate(dividend))).ToList();
        var creditAtTreatyRate = foreign.Sum(dividend => Math.Min(dividend.WithheldPln, dividend.GrossPln * Math.Min(rate, TreatyRate(dividend))));
        var kisCredit = Math.Min(p47, Pit38Amounts.Grosze(creditAtTreatyRate));
        var kisP49 = rounding.Tax(Math.Max(0m, p47 - kisCredit));
        var withholdingUnknown = foreign.Where(dividend => !dividend.WithholdingKnown && dividend.GrossPln > 0m).ToList();

        return new Pit38Dividends(p47, p48, p49, foreign, polish, overTreaty, kisCredit, kisP49, withholdingUnknown);
    }

    private static decimal TreatyRateOf(string? country) =>
        country is { } code && TreatyRates.TryGetValue(code, out var treaty) ? treaty : UsualTreatyRate;

    private static bool IsOverTreaty(DividendIncome dividend, decimal treatyRate) =>
        dividend.GrossPln > 0m && dividend.WithheldPln > Pit38Amounts.Grosze(dividend.GrossPln * treatyRate) + Grosz;
}
