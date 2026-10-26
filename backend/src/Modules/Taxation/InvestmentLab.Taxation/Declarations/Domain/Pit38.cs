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

    /// <summary>Treaty rates on portfolio dividends other than the usual 15% (US, DE, FR, CH, NL…): what KIS lets be deducted at most.</summary>
    private static readonly Dictionary<string, decimal> TreatyDividendRates = new() { ["JP"] = 0.10m, ["IT"] = 0.10m };

    private const decimal UsualTreatyDividendRate = 0.15m;

    public TaxDeclaration Fill(DeclarationInput input)
    {
        var (year, accounts, earlierLosses, countryOf) = (input.Year, input.Accounts, input.EarlierLosses, input.CountryOf);
        ITaxPolicy policy = new BelkaTaxPolicy();
        var taxed = accounts.Where(account => account.Policy.IsTaxed).ToList();
        var rate = policy.RateSchedule.Rate.Value;
        var rounding = policy.Rounding;

        decimal Sum(IEnumerable<DeclarationAccount> of, Func<TaxYearSummary, decimal> amount) => of.Sum(account => account.In(year) is { } summary ? amount(summary) : 0m);

        // A Polish broker reports the sales on PIT-8C (row 1): its amounts, once entered, stand instead of ours.
        var row1 = taxed.Where(account => PolishBrokers.IssuesPit8C(account.Broker)).ToList();
        var row2 = taxed.Except(row1).ToList();
        var withoutPit8C = row1.Where(account => account.Pit8C is null && account.In(year) is { Revenue: > 0m }).ToList();
        var p20 = row1.Sum(account => account.Pit8C?.Revenue ?? account.In(year)?.Revenue ?? 0m);
        var p21 = row1.Sum(account => account.Pit8C?.Costs ?? account.In(year)?.Costs ?? 0m);
        var (p22, p23) = (Sum(row2, s => s.Revenue), Sum(row2, s => s.Costs));
        var (p26, p27) = (p20 + p22, p21 + p23);
        var p28 = Math.Max(0m, p26 - p27);
        var p29 = Math.Max(0m, p27 - p26);

        var p30 = CarryforwardUsed(year, p26 - p27, taxed, earlierLosses, policy.LossCarryforward);
        var p31 = rounding.Base(p28 - p30);
        var p33 = Grosze(p31 * rate);
        const decimal p34 = 0m;
        var p35 = rounding.Tax(Math.Max(0m, p33 - p34));

        // Part G: every foreign dividend, whatever the broker — no Polish broker is the payer of tax on foreign dividends
        // (XTB, mBank say so). Dividends of Polish companies are taxed by the Polish payer and stay out.
        var foreign = taxed.SelectMany(account => account.Dividends).Where(dividend => countryOf(dividend.InstrumentId) != "PL").ToList();
        var polish = taxed.SelectMany(account => account.Dividends.Select(dividend => (Account: account, Dividend: dividend)))
            .Where(paid => countryOf(paid.Dividend.InstrumentId) == "PL").ToList();
        var p47 = Grosze(foreign.Sum(dividend => dividend.GrossPln) * rate);
        // The tax withheld abroad counts up to the Polish 19% of each dividend (art. 30a ust. 9, NSA II FSK 1171/22).
        var p48 = Math.Min(p47, Grosze(foreign.Sum(dividend => Math.Min(dividend.WithheldPln, dividend.GrossPln * rate))));
        var p49 = rounding.Tax(Math.Max(0m, p47 - p48));
        var p51 = p35 + p49;

        // KIS deducts the foreign tax only up to the treaty rate.
        decimal TreatyRate(DividendIncome dividend) =>
            countryOf(dividend.InstrumentId) is { } code && TreatyDividendRates.TryGetValue(code, out var treaty) ? treaty : UsualTreatyDividendRate;
        var overTreaty = foreign.Where(dividend => dividend.GrossPln > 0m && dividend.WithheldPln > dividend.GrossPln * TreatyRate(dividend)).ToList();
        var kisCredit = Math.Min(p47, Grosze(foreign.Sum(dividend => Math.Min(dividend.WithheldPln, dividend.GrossPln * Math.Min(rate, TreatyRate(dividend))))));
        var kisP49 = rounding.Tax(Math.Max(0m, p47 - kisCredit));
        var withholdingUnknown = foreign.Where(dividend => !dividend.WithholdingKnown && dividend.GrossPln > 0m).ToList();

        // PIT/ZG: one per country of the instruments sold abroad with a gain, while the person has income at all
        // (the e-Urząd form asks for it with any foreign income).
        bool IsCountry(string? code) => code is { Length: 2 } && code != "PL" && code != "EU" && !code.StartsWith('X');
        var foreignSales = row2.SelectMany(account => account.Sales).ToList();
        var attachments = p28 <= 0m ? [] : foreignSales
            .GroupBy(sale => countryOf(sale.InstrumentId))
            .Where(group => IsCountry(group.Key))
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
        var unknownCountry = foreignSales.Count(sale => !IsCountry(countryOf(sale.InstrumentId)) && countryOf(sale.InstrumentId) != "PL");

        var sections = new List<DeclarationSection>
        {
            new("C", "Dochody / straty – art. 30b ust. 1", [
                new(20, "Przychód – wiersz 1 (PIT-8C)", p20, Hint: row1.Count == 0 ? null : withoutPit8C.Count == 0 ? "poz. 35 з PIT-8C брокера" : "Наш розрахунок — введіть poz. 35 з PIT-8C"),
                new(21, "Koszty – wiersz 1 (PIT-8C)", p21, Hint: row1.Count == 0 ? null : withoutPit8C.Count == 0 ? "poz. 36 з PIT-8C брокера" : "Наш розрахунок — введіть poz. 36 з PIT-8C"),
                new(22, "Przychód – wiersz 2 (inne, z zagranicy)", p22, Hint: "Продажі брокерів без PIT-8C, за курсом NBP дня перед угодою"),
                new(23, "Koszty – wiersz 2", p23, Hint: "Купівля проданих лотів (FIFO) і комісії купівлі та продажу"),
                new(24, "Przychód – wiersz 3 (art. 21 ust. 1 pkt 105a)", 0m, Hint: "Пільга IPO — не застосовується"),
                new(25, "Koszty – wiersz 3", 0m),
                new(26, "Razem przychód", p26, Hint: "poz. 20 + 22 − 24", Computed: true),
                new(27, "Razem koszty", p27, Hint: "poz. 21 + 23 − 25", Computed: true),
                new(28, "Dochód", p28, Computed: true),
                new(29, "Strata", p29, Hint: p29 > 0m ? "Збиток можна віднімати від доходу наступних 5 років, до половини на рік" : null, Computed: true),
            ]),
            new("D", "Obliczenie zobowiązania podatkowego", [
                new(30, "Straty z lat ubiegłych", p30, Hint: "Не більше половини збитку кожного року за останні 5 років і не більше poz. 28"),
                new(31, "Podstawa obliczenia podatku", p31, Hint: "poz. 28 − 30, до повних злотих", Computed: true),
                new(32, "Stawka podatku (%)", decimal.Round(rate * 100m, 0), Computed: true),
                new(33, "Podatek od dochodów z poz. 31", p33, Computed: true),
                new(34, "Podatek zapłacony za granicą (art. 30b ust. 5a i 5b)", p34, Hint: "З продажів за кордоном податок не утримано"),
                new(35, "Podatek należny", p35, Hint: "poz. 33 − 34, до повних злотих", Computed: true),
            ]),
            new("G", "Podatek do zapłaty / nadpłata", [
                new(46, "Zryczałtowany podatek niepobrany przez płatnika", 0m),
                new(47, "Zryczałtowany podatek od przychodów zagranicznych (art. 30a ust. 1 pkt 1–5)", p47, Hint: "19% від іноземних дивідендів брутто (до утримання за кордоном), усіх брокерів"),
                new(48, "Podatek zapłacony za granicą (art. 30a ust. 9)", p48, Hint: "Утриманий за кордоном податок, не більше 19% кожного дивіденду"),
                new(49, "Różnica", p49, Hint: "poz. 47 − 48, до повних злотих", Computed: true),
                new(50, "Zaliczki przekazane spółce nieruchomościowej", 0m),
                new(51, "Podatek do zapłaty", p51, Hint: "poz. 35 + 45 + 46 + 49 − 50", Computed: true),
                new(52, "Nadpłata", 0m, Computed: true),
            ]),
            new("L", "Informacje o załącznikach", [
                new(72, "Liczba załączników PIT/ZG", attachments.Count, Hint: "Один PIT/ZG на кожну країну з доходом від продажів; дивіденди в нього не входять", Computed: true),
            ]),
        };

        var notes = new List<DeclarationNote>
        {
            new("Суми в PLN за середнім курсом NBP останнього робочого дня перед датою угоди чи виплати (art. 11a). Закон пов’язує дохід з переходом власності (art. 17 ust. 1ab) — частина брокерів і порадників бере курс дня перед датою розрахунку (T+1 США, T+2 ЄС), тоді суми трохи інші."),
            year == 2025
                ? new("Номери позицій — за формою PIT-38(18) і брошурою Міністерства фінансів за 2025 рік.")
                : new($"Номери позицій — за формою PIT-38(18) за 2025 рік; форми за {year} рік ще немає — перевірте номери, коли її опублікують.", true,
                    $"Номери poz. — за формою 2025 року: звірте з формою за {year}."),
            new("Частини E/F (криптовалюти) не заповнені: якщо маєте крипто, впишіть їх самі."),
        };
        if (withoutPit8C.Count > 0)
            notes.Add(new($"Рядок 1 — {string.Join(", ", withoutPit8C.Select(account => account.Name))}: введіть суми з PIT-8C брокера (poz. 35 і 36), і решта форми перерахується від них.", true,
                $"poz. 20/21: впишіть poz. 35/36 з PIT-8C ({string.Join(", ", withoutPit8C.Select(account => account.Broker).Distinct())}), тут — наш розрахунок."));
        if (withholdingUnknown.Count > 0)
            notes.Add(new($"{withholdingUnknown.Count} іноземних дивідендів без даних про утриманий за кордоном податок: poz. 47 рахується з отриманої суми, poz. 48 — без нього, тож податок завищено. Завантажте звіт брокера (Revolut P&L) або перевірте суми брутто й утримане самі. Якщо за кордоном нічого не утримали (напр. ірландські ETF), усе правильно.", true,
                $"{withholdingUnknown.Count} дивідендів без даних про утримане за кордоном: звірте poz. 47/48 з виписками брокера."));
        if (polish.Count > 0)
        {
            var abroad = polish.Any(paid => row2.Contains(paid.Account));
            notes.Add(new($"{polish.Count} дивідендів польських компаній не в розділі G: податок утримує польський платник." +
                (abroad ? " На іноземному брокері перевірте, що 19% справді утримали — інакше їх треба вписати в poz. 46." : ""), abroad,
                abroad ? "Дивіденди польських компаній на іноземному брокері: якщо 19% не утримали — впишіть у poz. 46." : null));
        }
        if (accounts.Any(account => !account.Policy.IsTaxed))
            notes.Add(new($"Не входять: {string.Join(", ", accounts.Where(account => !account.Policy.IsTaxed).Select(account => account.Name))} (IKE/IKZE не оподатковуються)."));
        if (p29 > 0m)
            notes.Add(new("За рік збиток: подайте PIT-38 усе одно — без нього збиток не можна буде відняти в наступні роки.", true,
                "Збиток: подайте PIT-38 усе одно, щоб відняти його в наступні роки."));
        if (p30 > 0m)
            notes.Add(new("poz. 30 — за правилом «до 50% збитку року». Закон дозволяє й раз за 5 років відняти до 5 000 000 zł одного збитку — тоді суму змініть самі."));
        if (attachments.Count > 0)
            notes.Add(new("PIT/ZG: країна — за ISIN цінного папера. Чи обов’язковий PIT/ZG, коли за кордоном податку з продажу не утримано, — спірно (брошура МФ пов’язує його з методом пропорційного відрахування), але e-Urząd вимагає його для доходу з-за кордону, і він нічого не змінює в податку."));
        if (unknownCountry > 0)
            notes.Add(new($"{unknownCountry} продажів без країни в ISIN (немає ISIN або XS/EU): їх немає в PIT/ZG — перевірте країну емітента.", true,
                $"{unknownCountry} продажів без країни в ISIN — їх немає в PIT/ZG."));
        if (overTreaty.Count > 0 && kisP49 != p49)
            notes.Add(new($"{overTreaty.Count} дивідендів з утриманням понад ставку договору (немає W-8BEN?). Тут залік до 19% (art. 30a ust. 9, NSA); податкова (KIS) зараховує лише за ставкою договору — тоді poz. 48 = {kisCredit:N2}, poz. 49 = {kisP49:N0} zł.", true,
                $"Утримано понад ставку договору: за позицією KIS poz. 48 = {kisCredit:N2}, poz. 49 = {kisP49:N0} zł."));

        var facts = new List<DeclarationFact>
        {
            new("Дивіденди з-за кордону брутто", foreign.Sum(dividend => dividend.GrossPln)),
            new("Утримано за кордоном з них", foreign.Sum(dividend => dividend.WithheldPln)),
        };

        string Treatment(DeclarationAccount account) =>
            !account.Policy.IsTaxed ? "Не оподатковується" : row1.Contains(account) ? "Рядок 1 (PIT-8C) · дивіденди в G" : "Рядок 2 · дивіденди в G";
        var lines = accounts.Select(account => account.In(year) is { } summary
            ? new DeclarationAccountLine(account.Name, account.Broker, Treatment(account), summary.Revenue, summary.Costs, summary.DividendIncome, summary.DividendTaxCredit,
                account.AccountId, row1.Contains(account), account.Pit8C)
            : new DeclarationAccountLine(account.Name, account.Broker, Treatment(account), 0m, 0m, 0m, 0m, account.AccountId, row1.Contains(account), account.Pit8C)).ToList();

        return new TaxDeclaration(Code, Title, year, Deadline(year), sections, attachments, lines, notes, p51, facts.Where(fact => fact.Amount != 0m).ToList());
    }

    /// <summary>30 April of the next year, moved to the next working day past a weekend, 1 May or 3 May (art. 12 § 5 Ordynacji).</summary>
    public static DateOnly Deadline(int year)
    {
        var day = new DateOnly(year + 1, 4, 30);
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || day is { Month: 5, Day: 1 or 3 })
            day = day.AddDays(1);
        return day;
    }

    private static decimal Grosze(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The person's yearly gains across accounts, with their losses deducted year after year up to <paramref name="year"/>.
    /// A loss the person entered for a year stands instead of what the journals computed for it (the same loss, once).
    /// </summary>
    private static decimal CarryforwardUsed(int year, decimal declaredGain, IReadOnlyList<DeclarationAccount> taxed, IReadOnlyList<LossYear> earlierLosses,
        ILossCarryforwardRule rule)
    {
        var gains = taxed.SelectMany(account => account.Years).GroupBy(summary => summary.Year).ToDictionary(group => group.Key, group => group.Sum(summary => summary.Gain));
        gains[year] = declaredGain;
        var entered = earlierLosses.Select(loss => loss.Year).ToHashSet();
        var pools = earlierLosses.Select(loss => new LossPool(loss)).ToList();
        var first = gains.Keys.Min();
        decimal used = 0m;
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

/// <summary>Polish brokers: they send PIT-8C for the sales (not for foreign dividends, which the taxpayer declares).</summary>
public static class PolishBrokers
{
    private static readonly string[] Names =
        ["XTB", "mBank", "mDM", "Bossa", "BOŚ", "PKO", "Pekao", "Santander", "ING", "Alior", "Millennium", "Noble", "BNP", "Trigon", "Ipopema", "Citi Handlowy", "BDM", "Michael/Ström", "Erste"];

    // Whole words: "Trading 212" is not ING.
    public static bool IssuesPit8C(string broker) =>
        Names.Any(name => Regex.IsMatch(broker, $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase));
}
