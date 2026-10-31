namespace InvestmentLab.Taxation.Declarations.Domain;

public static class Pit38Sections
{
    public static List<DeclarationSection> Of(Pit38Figures figures) =>
    [
        Income(figures),
        Tax(figures),
        Payment(figures),
        Attachments(figures),
    ];

    private static DeclarationSection Income(Pit38Figures figures)
    {
        var gains = figures.Gains;

        return new DeclarationSection("C", "Dochody / straty – art. 30b ust. 1", [
            new(
                20,
                "Przychód – wiersz 1 (PIT-8C)",
                gains.P20,
                Hint: figures.Rows.WithPit8CForm.Count == 0
                    ? null
                    : figures.Rows.BrokersMissingPit8C().Count == 0
                    ? "poz. 35 з PIT-8C брокера"
                    : "Наш розрахунок — введіть poz. 35 з PIT-8C"),
            new(
                21,
                "Koszty – wiersz 1 (PIT-8C)",
                gains.P21,
                Hint: figures.Rows.WithPit8CForm.Count == 0
                    ? null
                    : figures.Rows.BrokersMissingPit8C().Count == 0
                    ? "poz. 36 з PIT-8C брокера"
                    : "Наш розрахунок — введіть poz. 36 з PIT-8C"),
            new(22, "Przychód – wiersz 2 (inne, z zagranicy)", gains.P22, Hint: "Продажі брокерів без PIT-8C, за курсом NBP дня перед угодою"),
            new(23, "Koszty – wiersz 2", gains.P23, Hint: "Купівля проданих лотів (FIFO) і комісії купівлі та продажу"),
            new(24, "Przychód – wiersz 3 (art. 21 ust. 1 pkt 105a)", 0m, Hint: "Пільга IPO — не застосовується"),
            new(25, "Koszty – wiersz 3", 0m),
            new(26, "Razem przychód", gains.P26, Hint: "poz. 20 + 22 − 24", Computed: true),
            new(27, "Razem koszty", gains.P27, Hint: "poz. 21 + 23 − 25", Computed: true),
            new(28, "Dochód", gains.P28, Computed: true),
            new(
                29,
                "Strata",
                gains.P29,
                Hint: gains.P29 > 0m ? "Збиток можна віднімати від доходу наступних 5 років, до половини на рік" : null,
                Computed: true),
        ]);
    }

    private static DeclarationSection Tax(Pit38Figures figures)
    {
        var gains = figures.Gains;

        return new DeclarationSection("D", "Obliczenie zobowiązania podatkowego", [
            new(30, "Straty z lat ubiegłych", gains.P30, Hint: "Не більше половини збитку кожного року за останні 5 років і не більше poz. 28"),
            new(31, "Podstawa obliczenia podatku", gains.P31, Hint: "poz. 28 − 30, до повних злотих", Computed: true),
            new(32, "Stawka podatku (%)", decimal.Round(figures.Rate * 100m, 0)),
            new(33, "Podatek od dochodów z poz. 31", gains.P33, Computed: true),
            new(34, "Podatek zapłacony za granicą (art. 30b ust. 5a i 5b)", gains.P34, Hint: "З продажів за кордоном податок не утримано"),
            new(35, "Podatek należny", gains.P35, Hint: "poz. 33 − 34, до повних злотих", Computed: true),
        ]);
    }

    private static DeclarationSection Payment(Pit38Figures figures)
    {
        var gains = figures.Gains;
        var dividends = figures.Dividends;

        return new DeclarationSection("G", "Podatek do zapłaty / nadpłata", [
            new(46, "Zryczałtowany podatek niepobrany przez płatnika", 0m),
            new(
                47,
                "Zryczałtowany podatek od przychodów zagranicznych (art. 30a ust. 1 pkt 1–5)",
                dividends.P47,
                Hint: "19% від іноземних дивідендів брутто (до утримання за кордоном), усіх брокерів"),
            new(
                48,
                "Podatek zapłacony za granicą (art. 30a ust. 9)",
                dividends.P48,
                Hint: "Утриманий за кордоном податок, не більше 19% кожного дивіденду"),
            new(49, "Różnica", dividends.P49, Hint: "poz. 47 − 48, до повних злотих", Computed: true),
            new(50, "Zaliczki przekazane spółce nieruchomościowej", 0m),
            new(51, "Podatek do zapłaty", gains.P35 + dividends.P49, Hint: "poz. 35 + 45 + 46 + 49 − 50", Computed: true),
            new(52, "Nadpłata", 0m, Computed: true),
        ]);
    }

    private static DeclarationSection Attachments(Pit38Figures figures)
    {
        return new DeclarationSection("L", "Informacje o załącznikach", [
            new(
                72,
                "Liczba załączników PIT/ZG",
                figures.Attachments.Count,
                Hint: "Один PIT/ZG на кожну країну з доходом від продажів; дивіденди в нього не входять"),
        ]);
    }
}
