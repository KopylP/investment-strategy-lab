using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed class Pit38Form : ITaxDeclarationForm
{
    public string Code => "PIT-38";
    public string Title => "PIT-38 · Podatek Belki";
    public string Jurisdiction => TaxPolicies.Poland;

    public TaxDeclaration Fill(DeclarationInput input)
    {
        ITaxPolicy policy = new BelkaTaxPolicy();
        var taxed = input.Accounts.Where(account => account.Policy.IsTaxed).ToList();
        var rows = new Pit38BrokerRows(input.Year, taxed);

        var gains = Pit38CapitalGains.Of(input, rows, taxed, policy);
        var dividends = Pit38Dividends.Of(input, taxed, policy);
        var sales = new Pit38ForeignSales(rows.WithoutPit8CForm, input.CountryOf);
        var attachments = sales.AttachmentsWhen(gains.P28);

        var figures = new Pit38Figures(input, rows, gains, dividends, policy.RateSchedule.Rate.Value, attachments, sales.WithUnknownCountry);

        return new TaxDeclaration(
            Code,
            Title,
            input.Year,
            Deadline(input.Year),
            Pit38Sections.Of(figures),
            attachments,
            AccountLinesOf(input, rows),
            Pit38Notes.Of(figures),
            dividends.P49 + gains.P35,
            FactsOf(dividends.Foreign));
    }

    private static List<DeclarationAccountLine> AccountLinesOf(DeclarationInput input, Pit38BrokerRows rows) =>
        [.. input.Accounts.Select(account => AccountLineOf(account, input.Year, rows))];

    private static DeclarationAccountLine AccountLineOf(DeclarationAccount account, int year, Pit38BrokerRows rows)
    {
        var summary = account.In(year);

        return new DeclarationAccountLine(
            account.Name,
            account.Broker,
            TreatmentOf(account, rows),
            summary?.Revenue ?? 0m,
            summary?.Costs ?? 0m,
            summary?.DividendIncome ?? 0m,
            summary?.DividendTaxCredit ?? 0m,
            account.AccountId,
            rows.TakesPit8C(account),
            account.Pit8COf(year),
            rows.Pit8CCoveredBy(account));
    }

    private static string TreatmentOf(DeclarationAccount account, Pit38BrokerRows rows)
    {
        if (!account.Policy.IsTaxed)
            return "Не оподатковується";

        return rows.TakesPit8C(account) ? "Рядок 1 (PIT-8C) · дивіденди в G" : "Рядок 2 · дивіденди в G";
    }

    private static List<DeclarationFact> FactsOf(IReadOnlyList<DividendIncome> foreign)
    {
        List<DeclarationFact> facts =
        [
            new("Дивіденди з-за кордону брутто", foreign.Sum(dividend => dividend.GrossPln)),
            new("Утримано за кордоном з них (у poz. 48 — не більше 19% кожного)", foreign.Sum(dividend => dividend.WithheldPln)),
        ];

        return [.. facts.Where(fact => fact.Amount != 0m)];
    }

    public static DateOnly Deadline(int year)
    {
        var day = new DateOnly(year + 1, 4, 30);

        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || day is { Month: 5, Day: 1 or 3 })
            day = day.AddDays(1);

        return day;
    }
}
