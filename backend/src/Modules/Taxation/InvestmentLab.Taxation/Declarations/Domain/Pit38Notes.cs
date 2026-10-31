namespace InvestmentLab.Taxation.Declarations.Domain;

public static class Pit38Notes
{
    public static List<DeclarationNote> Of(Pit38Figures figures)
    {
        var missingPit8C = figures.Rows.BrokersMissingPit8C();
        var missingBrokers = string.Join(", ", missingPit8C);
        var untaxed = figures.Input.Accounts.Where(account => !account.Policy.IsTaxed).Select(account => account.Name);

        var notes = new List<DeclarationNote>
        {
            new("Суми в PLN за середнім курсом NBP останнього робочого дня перед датою угоди чи виплати (art. 11a). " +
                "Закон пов’язує дохід з переходом власності (art. 17 ust. 1ab) — частина брокерів і порадників бере " +
                "курс дня перед датою розрахунку (T+1 США, T+2 ЄС), тоді суми трохи інші."),
            figures.Input.Year == 2025
                ? new("Номери позицій — за формою PIT-38(18) і брошурою Міністерства фінансів за 2025 рік.")
                : new($"Номери позицій — за формою PIT-38(18) за 2025 рік; форми за {figures.Input.Year} рік ще немає — " +
                    "перевірте номери, коли її опублікують.", true,
                    $"Номери poz. — за формою 2025 року: звірте з формою за {figures.Input.Year}."),
            new("Частини E/F (криптовалюти) не заповнені: якщо маєте крипто, впишіть їх самі."),
        };

        if (missingPit8C.Count > 0)
            notes.Add(new(
                $"Рядок 1 — {missingBrokers}: введіть суми з PIT-8C брокера (poz. 35 і 36; один PIT-8C на всі ваші рахунки в цього брокера), " +
                    $"і решта форми перерахується від них.",
                true,
                $"poz. 20/21: впишіть poz. 35/36 з PIT-8C ({missingBrokers}), тут — наш розрахунок."));

        if (figures.Rows.WithPit8CForm.Any(account => account.Dividends.Any(dividend => figures.Input.CountryOf(dividend.InstrumentId) != "PL")))
            notes.Add(new("Іноземні дивіденди з польського брокера — у розділі G: брокер не є їх платником. Виняток — акції " +
                "іноземних компаній на GPW, з яких брокер сам утримав 19% (видно у виписці): такі приберіть із poz. " +
                "47/48."));

        if (figures.Dividends.WithholdingUnknown.Count > 0)
            notes.Add(new("Іноземні дивіденди без даних про утриманий за кордоном податок " +
                $"({figures.Dividends.WithholdingUnknown.Count}): poz. 47 рахується з отриманої суми, poz. 48 — без " +
                "нього, тож податок завищено. Завантажте звіт брокера (Revolut P&L) або перевірте суми брутто й " +
                "утримане самі. Якщо за кордоном нічого не утримали (напр. ірландські ETF), усе правильно.", true,
                $"Дивіденди без даних про утримане за кордоном ({figures.Dividends.WithholdingUnknown.Count}): звірте " +
                    "poz. 47/48 з виписками брокера."));

        if (figures.Dividends.Polish.Count > 0)
        {
            var abroad = figures.Dividends.Polish.Any(paid => figures.Rows.WithoutPit8CForm.Contains(paid.Account));
            notes.Add(new($"{figures.Dividends.Polish.Count} дивідендів польських компаній не в розділі G: податок утримує польський платник." +
                (abroad ? " На іноземному брокері перевірте, що 19% справді утримали — інакше їх треба вписати в poz. 46." : ""), abroad,
                abroad ? "Дивіденди польських компаній на іноземному брокері: якщо 19% не утримали — впишіть у poz. 46." : null));
        }

        if (figures.Input.Accounts.Any(account => !account.Policy.IsTaxed))
            notes.Add(new($"Не входять: {string.Join(", ", untaxed)} (IKE/IKZE не оподатковуються)."));

        if (figures.Gains.P29 > 0m)
            notes.Add(new("За рік збиток: подайте PIT-38 усе одно — без нього збиток не можна буде відняти в наступні роки.", true,
                "Збиток: подайте PIT-38 усе одно, щоб відняти його в наступні роки."));

        if (figures.Gains.P30 > 0m)
            notes.Add(new("poz. 30 — за правилом «до 50% збитку року» і з припущенням, що в кожній минулій декларації відняли " +
                "стільки, скільки дозволено. Якщо відняли менше або не подавали — сума інша. Закон дозволяє й раз за " +
                "5 років відняти до 5 000 000 zł одного збитку — тоді суму змініть самі."));

        if (figures.Attachments.Count > 0)
            notes.Add(new("PIT/ZG: країна — за ISIN цінного папера. Чи обов’язковий PIT/ZG, коли за кордоном податку з продажу " +
                "не утримано, — спірно (брошура МФ пов’язує його з методом пропорційного відрахування), але e-Urząd " +
                "вимагає його для доходу з-за кордону, і він нічого не змінює в податку."));

        if (figures.SalesWithUnknownCountry > 0)
            notes.Add(new($"{figures.SalesWithUnknownCountry} продажів без країни в ISIN (немає ISIN або XS/EU): їх немає в " +
                "PIT/ZG — перевірте країну емітента.", true,
                $"{figures.SalesWithUnknownCountry} продажів без країни в ISIN — їх немає в PIT/ZG."));

        if (figures.Dividends.OverTreaty.Count > 0 && figures.Dividends.KisP49 != figures.Dividends.P49)
            notes.Add(new($"{figures.Dividends.OverTreaty.Count} дивідендів з утриманням понад ставку договору (немає W-8BEN?). " +
                "Тут залік до 19% (art. 30a ust. 9, NSA); податкова (KIS) зараховує лише за ставкою договору — тоді " +
                $"poz. 48 = {Pit38Amounts.Typed(figures.Dividends.KisCredit)}, poz. 49 = " +
                $"{Pit38Amounts.Typed(figures.Dividends.KisP49)} zł.", true,
                "Утримано понад ставку договору: за позицією KIS poz. 48 = " +
                    $"{Pit38Amounts.Typed(figures.Dividends.KisCredit)}, poz. 49 = " +
                    $"{Pit38Amounts.Typed(figures.Dividends.KisP49)} zł."));

        return notes;
    }
}
