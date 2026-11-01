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

    private static string? Pit8CHint(Pit38Figures figures, string fromBroker, string ours)
    {
        if (figures.Rows.WithPit8CForm.Count == 0)
            return null;

        return Message.Of(figures.Rows.BrokersMissingPit8C().Count == 0 ? fromBroker : ours);
    }

    private static DeclarationSection Income(Pit38Figures figures)
    {
        var gains = figures.Gains;

        return new DeclarationSection("C", "Dochody / straty – art. 30b ust. 1", [
            new(
                20,
                "Przychód – wiersz 1 (PIT-8C)",
                gains.P20,
                Hint: Pit8CHint(figures, "pit38.hint.p20_broker", "pit38.hint.p20_ours")),
            new(
                21,
                "Koszty – wiersz 1 (PIT-8C)",
                gains.P21,
                Hint: Pit8CHint(figures, "pit38.hint.p21_broker", "pit38.hint.p21_ours")),
            new(22, "Przychód – wiersz 2 (inne, z zagranicy)", gains.P22, Hint: Message.Of("pit38.hint.p22")),
            new(23, "Koszty – wiersz 2", gains.P23, Hint: Message.Of("pit38.hint.p23")),
            new(24, "Przychód – wiersz 3 (art. 21 ust. 1 pkt 105a)", 0m, Hint: Message.Of("pit38.hint.p24")),
            new(25, "Koszty – wiersz 3", 0m),
            new(26, "Razem przychód", gains.P26, Hint: "poz. 20 + 22 − 24", Computed: true),
            new(27, "Razem koszty", gains.P27, Hint: "poz. 21 + 23 − 25", Computed: true),
            new(28, "Dochód", gains.P28, Computed: true),
            new(
                29,
                "Strata",
                gains.P29,
                Hint: gains.P29 > 0m ? Message.Of("pit38.hint.p29") : null,
                Computed: true),
        ]);
    }

    private static DeclarationSection Tax(Pit38Figures figures)
    {
        var gains = figures.Gains;

        return new DeclarationSection("D", "Obliczenie zobowiązania podatkowego", [
            new(30, "Straty z lat ubiegłych", gains.P30, Hint: Message.Of("pit38.hint.p30")),
            new(31, "Podstawa obliczenia podatku", gains.P31, Hint: Message.Of("pit38.hint.p31"), Computed: true),
            new(32, "Stawka podatku (%)", decimal.Round(figures.Rate * 100m, 0)),
            new(33, "Podatek od dochodów z poz. 31", gains.P33, Computed: true),
            new(34, "Podatek zapłacony za granicą (art. 30b ust. 5a i 5b)", gains.P34, Hint: Message.Of("pit38.hint.p34")),
            new(35, "Podatek należny", gains.P35, Hint: Message.Of("pit38.hint.p35"), Computed: true),
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
                Hint: Message.Of("pit38.hint.p47")),
            new(
                48,
                "Podatek zapłacony za granicą (art. 30a ust. 9)",
                dividends.P48,
                Hint: Message.Of("pit38.hint.p48")),
            new(49, "Różnica", dividends.P49, Hint: Message.Of("pit38.hint.p49"), Computed: true),
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
                Hint: Message.Of("pit38.hint.p72")),
        ]);
    }
}
