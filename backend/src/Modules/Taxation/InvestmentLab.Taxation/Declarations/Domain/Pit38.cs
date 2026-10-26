using System.Text.RegularExpressions;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

/// <summary>
/// PIT-38 of one person for one year, from all of their accounts: the form is per taxpayer, so gains and losses of
/// different brokers net in part C, and losses of earlier years (5 years, at most half of each a year) are deducted
/// from the netted gain, not account by account. Positions follow PIT-38(18) and the Ministry's brochure for 2025.
/// </summary>
public sealed class Pit38Form : ITaxDeclarationForm
{
    public string Code => "PIT-38";
    public string Title => "PIT-38 · Podatek Belki";
    public string Jurisdiction => TaxPolicies.Poland;

    /// <summary>Rate of most Polish treaties on portfolio dividends (US, DE, FR, CH, NL…): what KIS lets be deducted at most.</summary>
    private const decimal TreatyDividendRate = 0.15m;

    public TaxDeclaration Fill(DeclarationInput input)
    {
        var (year, accounts, earlierLosses, countryOf) = (input.Year, input.Accounts, input.EarlierLosses, input.CountryOf);
        ITaxPolicy policy = new BelkaTaxPolicy();
        var taxed = accounts.Where(account => account.Policy.IsTaxed).ToList();
        var rate = policy.RateSchedule.Rate.Value;
        var rounding = policy.Rounding;

        decimal Sum(IEnumerable<DeclarationAccount> of, Func<TaxYearSummary, decimal> amount) => of.Sum(account => account.In(year) is { } summary ? amount(summary) : 0m);

        // A Polish broker reports the sales on PIT-8C (row 1) and withholds the tax on dividends itself (not in part G).
        var row1 = taxed.Where(account => PolishBrokers.IssuesPit8C(account.Broker)).ToList();
        var row2 = taxed.Except(row1).ToList();
        var (p20, p21) = (Sum(row1, s => s.Revenue), Sum(row1, s => s.Costs));
        var (p22, p23) = (Sum(row2, s => s.Revenue), Sum(row2, s => s.Costs));
        var (p26, p27) = (p20 + p22, p21 + p23);
        var p28 = Math.Max(0m, p26 - p27);
        var p29 = Math.Max(0m, p27 - p26);

        var p30 = CarryforwardUsed(year, taxed, earlierLosses, policy.LossCarryforward);
        var p31 = rounding.Base(p28 - p30);
        var p33 = decimal.Round(p31 * rate, 2, MidpointRounding.AwayFromZero);
        const decimal p34 = 0m;
        var p35 = rounding.Tax(Math.Max(0m, p33 - p34));

        // Part G: foreign dividends no Polish payer taxed; the tax withheld abroad is already capped at the Polish rate per dividend.
        var p47 = decimal.Round(Sum(row2, s => s.DividendIncome) * rate, 2, MidpointRounding.AwayFromZero);
        var p48 = Math.Min(p47, Sum(row2, s => s.DividendTaxCredit));
        var p49 = rounding.Tax(Math.Max(0m, p47 - p48));
        var p51 = p35 + p49;

        // PIT/ZG: one per country of the instruments sold abroad with a gain (the e-Urząd form asks for it with any foreign income).
        var attachments = row2.SelectMany(account => account.Sales)
            .GroupBy(sale => countryOf(sale.InstrumentId))
            .Where(group => group.Key is { Length: 2 } code && code != "PL")
            .Select(group => (Code: group.Key!, Income: group.Sum(sale => sale.GainPln)))
            .Where(country => country.Income > 0m)
            .OrderBy(country => country.Code)
            .Select(country => new DeclarationAttachment("PIT/ZG", $"PIT/ZG · {Countries.Name(country.Code)}", [
                new(6, "Państwo uzyskania dochodu", null, Countries.Name(country.Code)),
                new(7, "Kod kraju", null, country.Code),
                new(29, "Dochód, o którym mowa w art. 30b ust. 5a i 5b", country.Income, Hint: "Прибуток від продажу паперів цієї країни (за ISIN)"),
                new(30, "Podatek zapłacony za granicą od dochodów z poz. 29", 0m, Hint: "Брокер податку з продажу не утримує"),
            ]))
            .ToList();
        var unknownCountry = row2.SelectMany(account => account.Sales).Count(sale => countryOf(sale.InstrumentId) is not { Length: 2 });

        // KIS deducts the foreign tax only up to the treaty rate; the courts (NSA, 2023 and 2025) up to the Polish 19%.
        var overTreaty = row2.SelectMany(account => account.Dividends).Where(dividend => dividend.GrossPln > 0m && dividend.WithheldPln > dividend.GrossPln * TreatyDividendRate).ToList();
        var kisCredit = Math.Min(p47, Sum(row2, s => s.DividendTaxCredit) - overTreaty.Sum(dividend => Math.Min(dividend.WithheldPln, dividend.GrossPln * rate) - dividend.GrossPln * TreatyDividendRate));
        var kisP49 = rounding.Tax(Math.Max(0m, p47 - kisCredit));

        var sections = new List<DeclarationSection>
        {
            new("C", "Dochody / straty – art. 30b ust. 1", [
                new(20, "Przychód – wiersz 1 (PIT-8C)", p20, Hint: row1.Count > 0 ? "Сума poz. 35 з PIT-8C брокера; тут — наш розрахунок, звірте" : null),
                new(21, "Koszty – wiersz 1 (PIT-8C)", p21, Hint: row1.Count > 0 ? "Сума poz. 36 з PIT-8C брокера; тут — наш розрахунок, звірте" : null),
                new(22, "Przychód – wiersz 2 (inne, z zagranicy)", p22, Hint: "Продажі брокерів без PIT-8C, за курсом NBP дня перед угодою"),
                new(23, "Koszty – wiersz 2", p23, Hint: "Купівля проданих лотів (FIFO) і комісії купівлі та продажу"),
                new(24, "Przychód – wiersz 3 (art. 21 ust. 1 pkt 105a)", 0m, Hint: "Пільга IPO — не застосовується"),
                new(25, "Koszty – wiersz 3", 0m),
                new(26, "Razem przychód", p26, Hint: "poz. 20 + 22 − 24"),
                new(27, "Razem koszty", p27, Hint: "poz. 21 + 23 − 25"),
                new(28, "Dochód", p28),
                new(29, "Strata", p29, Hint: p29 > 0m ? "Збиток можна віднімати від доходу наступних 5 років, до половини на рік" : null),
            ]),
            new("D", "Obliczenie zobowiązania podatkowego", [
                new(30, "Straty z lat ubiegłych", p30, Hint: "Не більше половини збитку кожного року за останні 5 років і не більше poz. 28"),
                new(31, "Podstawa obliczenia podatku", p31, Hint: "poz. 28 − 30, до повних злотих"),
                new(32, "Stawka podatku (%)", decimal.Round(rate * 100m, 0)),
                new(33, "Podatek od dochodów z poz. 31", p33),
                new(34, "Podatek zapłacony za granicą (art. 30b ust. 5a i 5b)", p34, Hint: "З продажів за кордоном податок не утримано"),
                new(35, "Podatek należny", p35, Hint: "poz. 33 − 34, до повних злотих"),
            ]),
            new("G", "Podatek do zapłaty / nadpłata", [
                new(46, "Zryczałtowany podatek niepobrany przez płatnika", 0m),
                new(47, "Zryczałtowany podatek od przychodów zagranicznych (art. 30a ust. 1 pkt 1–5)", p47, Hint: "19% від дивідендів брутто (до утримання за кордоном)"),
                new(48, "Podatek zapłacony za granicą (art. 30a ust. 9)", p48, Hint: "Утриманий за кордоном податок, не більше 19% кожного дивіденду"),
                new(49, "Różnica", p49, Hint: "poz. 47 − 48, до повних злотих"),
                new(50, "Zaliczki przekazane spółce nieruchomościowej", 0m),
                new(51, "Podatek do zapłaty", p51, Hint: "poz. 35 + 45 + 46 + 49 − 50"),
                new(52, "Nadpłata", 0m),
            ]),
            new("L", "Informacje o załącznikach", [
                new(72, "Liczba załączników PIT/ZG", attachments.Count, Hint: "Один PIT/ZG на кожну країну з доходом від продажів; дивіденди в нього не входять"),
            ]),
        };

        var notes = new List<string>
        {
            "Суми в PLN за середнім курсом NBP останнього робочого дня перед угодою чи виплатою (art. 11a).",
            "Номери позицій — за формою PIT-38(18) і брошурою Міністерства фінансів за 2025 рік." + (year == 2025 ? "" : $" Перевірте їх у формі за {year} рік."),
        };
        if (row1.Count > 0)
            notes.Add($"Рядок 1 — брокери з PIT-8C ({string.Join(", ", row1.Select(account => account.Broker).Distinct())}): впишіть суми з PIT-8C, а не наші; їхні дивіденди оподатковує сам брокер, вони не в розділі G.");
        if (accounts.Any(account => !account.Policy.IsTaxed))
            notes.Add($"Не входять: {string.Join(", ", accounts.Where(account => !account.Policy.IsTaxed).Select(account => account.Name))} (IKE/IKZE не оподатковуються).");
        if (p29 > 0m)
            notes.Add("За рік збиток: подайте PIT-38 усе одно — без нього збиток не можна буде відняти в наступні роки.");
        if (p30 > 0m)
            notes.Add("poz. 30 — за правилом «до 50% збитку року». Закон дозволяє й раз за 5 років відняти до 5 000 000 zł одного збитку — тоді суму змініть самі.");
        if (attachments.Count > 0)
            notes.Add("PIT/ZG: країна — за ISIN цінного папера. Чи обов’язковий PIT/ZG, коли за кордоном податку з продажу не утримано, — спірно (брошура МФ пов’язує його з методом пропорційного відрахування), але e-Urząd вимагає його для доходу з-за кордону, і він нічого не змінює в податку.");
        if (unknownCountry > 0)
            notes.Add($"{unknownCountry} продажів без ISIN: їхньої країни немає в PIT/ZG — додайте ISIN інструменту.");
        if (overTreaty.Count > 0 && kisP49 != p49)
            notes.Add($"{overTreaty.Count} дивідендів з утриманням понад 15% (немає W-8BEN?). Тут залік до 19% (позиція NSA); податкова (KIS) зараховує лише за ставкою договору — тоді poz. 48 = {kisCredit:N2}, poz. 49 = {kisP49:N0} zł.");

        string Treatment(DeclarationAccount account) =>
            !account.Policy.IsTaxed ? "Не оподатковується" : row1.Contains(account) ? "Рядок 1 (PIT-8C)" : "Рядок 2 · дивіденди в G";
        var lines = accounts.Select(account => account.In(year) is { } summary
            ? new DeclarationAccountLine(account.Name, account.Broker, Treatment(account), summary.Revenue, summary.Costs, summary.DividendIncome, summary.DividendTaxCredit)
            : new DeclarationAccountLine(account.Name, account.Broker, Treatment(account), 0m, 0m, 0m, 0m)).ToList();

        return new TaxDeclaration(Code, Title, year, new DateOnly(year + 1, 4, 30), sections, attachments, lines, notes);
    }

    /// <summary>The person's yearly gains across accounts, with their losses deducted year after year up to <paramref name="year"/>.</summary>
    private static decimal CarryforwardUsed(int year, IReadOnlyList<DeclarationAccount> taxed, IReadOnlyList<LossYear> earlierLosses, ILossCarryforwardRule rule)
    {
        var gains = taxed.SelectMany(account => account.Years).GroupBy(summary => summary.Year).ToDictionary(group => group.Key, group => group.Sum(summary => summary.Gain));
        var pools = earlierLosses.Select(loss => new LossPool(loss)).ToList();
        var first = gains.Keys.Append(year).Min();
        decimal used = 0m;
        for (var number = first; number <= year; number++)
        {
            var gain = gains.GetValueOrDefault(number);
            used = LossPool.UseAgainst(pools, number, gain, rule).Sum(use => use.Amount);
            if (gain < 0m)
                pools.Add(new LossPool(new LossYear(number, -gain)));
        }
        return used;
    }
}

/// <summary>Polish names of the countries PIT/ZG asks for (poz. 6), by ISO code; others show the code.</summary>
public static class Countries
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["US"] = "Stany Zjednoczone Ameryki", ["IE"] = "Irlandia", ["LU"] = "Luksemburg", ["DE"] = "Niemcy", ["NL"] = "Holandia", ["GB"] = "Wielka Brytania",
        ["FR"] = "Francja", ["CH"] = "Szwajcaria", ["JP"] = "Japonia", ["CA"] = "Kanada", ["ES"] = "Hiszpania", ["IT"] = "Włochy", ["BE"] = "Belgia",
        ["AT"] = "Austria", ["DK"] = "Dania", ["SE"] = "Szwecja", ["FI"] = "Finlandia", ["NO"] = "Norwegia", ["PT"] = "Portugalia", ["KY"] = "Kajmany",
        ["BM"] = "Bermudy", ["JE"] = "Jersey", ["CN"] = "Chiny", ["HK"] = "Hongkong", ["TW"] = "Tajwan", ["KR"] = "Korea Południowa", ["AU"] = "Australia",
        ["IL"] = "Izrael", ["LT"] = "Litwa", ["CZ"] = "Czechy",
    };

    public static string Name(string code) => Names.GetValueOrDefault(code, code);
}

/// <summary>Brokers that are Polish tax remitters: they send PIT-8C and withhold the tax on dividends.</summary>
public static class PolishBrokers
{
    private static readonly string[] Names =
        ["XTB", "mBank", "mDM", "Bossa", "BOŚ", "PKO", "Pekao", "Santander", "ING", "Alior", "Millennium", "Noble", "BNP", "Trigon", "Ipopema", "Citi Handlowy"];

    // Whole words: "Trading 212" is not ING.
    public static bool IssuesPit8C(string broker) =>
        Names.Any(name => Regex.IsMatch(broker, $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase));
}
