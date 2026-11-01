using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class Pit38FormTests
{
    private static readonly Guid Apple = Guid.NewGuid(), Vwce = Guid.NewGuid(), Orlen = Guid.NewGuid(), Asml = Guid.NewGuid(), NoIsin = Guid.NewGuid(), Toyota = Guid.NewGuid(),
        Eurobond = Guid.NewGuid();
    private static readonly Pit38Form Form = new();

    private static string Rendered(string text) => MessageLocalizer.Default.Render(text);
    private static readonly ITaxPolicy Belka = new BelkaTaxPolicy(), Ike = TaxPolicies.For("PL-IKE");

    private static TaxYearSummary Year(int year, decimal revenue, decimal costs, decimal dividends = 0m, decimal credit = 0m) =>
        new(year, revenue, costs, revenue - costs, 0m, 0m, 0m, dividends, 0m, 0m, 0m, credit);

    private static Realization Sale(Guid instrument, decimal gain) =>
        new(Guid.NewGuid(), new DateOnly(2025, 6, 1), instrument, 1m, 0m, 0m, gain, 4m, null, [], [], 0m);

    private static DividendIncome Paid(decimal gross, decimal withheld, Guid? instrument = null, bool known = true) =>
        new(Guid.NewGuid(), new DateOnly(2025, 5, 1), instrument ?? Apple, gross - withheld, gross - withheld, 1m, GrossInTaxCurrency: gross, WithheldInTaxCurrency: withheld, WithholdingKnown: known);

    private static DeclarationAccount Account(string broker, IReadOnlyList<TaxYearSummary> years, IReadOnlyList<Realization>? sales = null,
        IReadOnlyList<DividendIncome>? dividends = null, ITaxPolicy? policy = null, Pit8CAmounts? pit8C = null, string? name = null,
        IReadOnlyDictionary<int, Pit8CAmounts>? pit8CByYear = null) =>
        new(name ?? $"{broker} EUR", broker, policy ?? Belka, years, sales ?? [], dividends ?? [], Guid.NewGuid(),
            pit8CByYear ?? (pit8C is null ? null : new Dictionary<int, Pit8CAmounts> { [2025] = pit8C }));

    private static string? Country(Guid id) =>
        id == Apple ? "US" : id == Vwce ? "IE" : id == Orlen ? "PL" : id == Asml ? "NL" : id == Toyota ? "JP" : id == Eurobond ? "XS" : null;

    private static TaxDeclaration Fill(params DeclarationAccount[] accounts) => Fill(2025, [], accounts);

    private static TaxDeclaration Fill(int year, IReadOnlyList<LossYear> losses, params DeclarationAccount[] accounts) =>
        Form.Fill(new DeclarationFacts(year, accounts, losses, Country));

    private static decimal? Poz(TaxDeclaration declaration, int position) =>
        declaration.Sections.SelectMany(section => section.Fields).Single(field => field.Position == position).Amount;

    [Fact]
    public void Brokers_with_PIT_8C_go_to_row_1_others_to_row_2_and_their_gains_and_losses_net()
    {
        var declaration = Fill(
            Account("XTB", [Year(2025, 5000m, 5400m)]),
            Account("Revolut", [Year(2025, 10000m, 9000m)]));

        (Poz(declaration, 20), Poz(declaration, 21)).ShouldBe((5000m, 5400m));
        (Poz(declaration, 22), Poz(declaration, 23)).ShouldBe((10000m, 9000m));
        (Poz(declaration, 24), Poz(declaration, 25)).ShouldBe((0m, 0m));
        (Poz(declaration, 26), Poz(declaration, 27)).ShouldBe((15000m, 14400m));
        (Poz(declaration, 28), Poz(declaration, 29)).ShouldBe((600m, 0m));
        Poz(declaration, 31).ShouldBe(600m);
        Poz(declaration, 32).ShouldBe(19m);
        Poz(declaration, 33).ShouldBe(114.00m);
        Poz(declaration, 34).ShouldBe(0m);
        Poz(declaration, 35).ShouldBe(114m);
        Poz(declaration, 51).ShouldBe(114m);
        Poz(declaration, 52).ShouldBe(0m);
    }

    [Fact]
    public void Several_foreign_brokers_add_up_in_row_2()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 1000m, 800m)]), Account("Interactive Brokers", [Year(2025, 3000m, 2500m)]));

        (Poz(declaration, 20), Poz(declaration, 21)).ShouldBe((0m, 0m));
        (Poz(declaration, 22), Poz(declaration, 23)).ShouldBe((4000m, 3300m));
        Poz(declaration, 28).ShouldBe(700m);
    }

    [Fact]
    public void A_year_with_a_loss_shows_it_in_poz_29_leaves_part_D_at_zero_and_says_to_file_anyway()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 1000m, 1500m)]));

        (Poz(declaration, 28), Poz(declaration, 29)).ShouldBe((0m, 500m));
        Poz(declaration, 30).ShouldBe(0m);
        Poz(declaration, 31).ShouldBe(0m);
        Poz(declaration, 35).ShouldBe(0m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("збиток"));
    }

    [Theory]
    [InlineData("600.49", 600, "114.00", 114)]
    [InlineData("600.50", 601, "114.19", 114)]
    [InlineData("602.50", 603, "114.57", 115)]
    public void The_base_is_whole_zloty_the_tax_has_grosze_and_the_tax_due_is_whole_zloty_half_rounding_up(string gain, int p31, string p33, int p35)
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 1000m + Parse(gain), 1000m)]));

        Poz(declaration, 31).ShouldBe(p31);
        Poz(declaration, 33).ShouldBe(Parse(p33));
        Poz(declaration, 35).ShouldBe(p35);
    }

    [Fact]
    public void Earlier_losses_are_the_persons_netted_across_accounts_and_deducted_at_most_half_a_year()
    {
        var declaration = Fill(
            Account("Revolut", [Year(2024, 1000m, 2000m), Year(2025, 3000m, 1000m)]),
            Account("Trading 212", [Year(2024, 700m, 500m)]));

        Poz(declaration, 28).ShouldBe(2000m);
        Poz(declaration, 30).ShouldBe(400m);
        Poz(declaration, 31).ShouldBe(1600m);
        Poz(declaration, 35).ShouldBe(304m);
    }

    [Fact]
    public void A_loss_of_one_account_in_a_year_another_account_gained_more_is_no_loss_to_carry()
    {
        var declaration = Fill(
            Account("Revolut", [Year(2024, 0m, 1000m), Year(2025, 1000m, 0m)]),
            Account("Trading 212", [Year(2024, 1500m, 0m)]));

        Poz(declaration, 30).ShouldBe(0m);
        Poz(declaration, 31).ShouldBe(1000m);
    }

    [Fact]
    public void Losses_entered_by_the_user_count_with_what_was_already_used_and_expire_after_five_years()
    {
        var declaration = Fill(2025, [new LossYear(2019, 5000m), new LossYear(2022, 3000m, AlreadyUsed: 2000m)], Account("Revolut", [Year(2025, 5000m, 1000m)]));

        Poz(declaration, 30).ShouldBe(1000m);
        Poz(declaration, 31).ShouldBe(3000m);
    }

    [Fact]
    public void A_loss_is_usable_in_its_fifth_year_and_older_losses_go_first()
    {
        var declaration = Fill(2025, [new LossYear(2020, 1000m), new LossYear(2023, 1000m)], Account("Revolut", [Year(2025, 700m, 100m)]));

        Poz(declaration, 30).ShouldBe(600m);
        Poz(declaration, 31).ShouldBe(0m);
    }

    [Fact]
    public void The_deduction_never_exceeds_the_years_income()
    {
        var declaration = Fill(Account("Revolut", [Year(2024, 0m, 10000m), Year(2025, 1000m, 0m)]));

        Poz(declaration, 30).ShouldBe(1000m);
        Poz(declaration, 31).ShouldBe(0m);
        Poz(declaration, 35).ShouldBe(0m);
    }

    [Fact]
    public void What_earlier_years_deducted_is_no_longer_there()
    {
        var declaration = Fill(Account("Revolut", [Year(2022, 0m, 1000m), Year(2023, 300m, 0m), Year(2024, 1000m, 0m), Year(2025, 1000m, 0m)]));

        Poz(declaration, 30).ShouldBe(200m);
        Poz(declaration, 31).ShouldBe(800m);
    }

    [Fact]
    public void A_loss_entered_for_a_year_the_journal_also_has_counts_once()
    {
        var declaration = Fill(2025, [new LossYear(2022, 1000m)], Account("Revolut", [Year(2022, 0m, 1000m), Year(2025, 5000m, 0m)]));

        Poz(declaration, 30).ShouldBe(500m);
    }

    [Fact]
    public void Half_of_an_odd_loss_is_rounded_down_never_above_50_percent()
    {
        var declaration = Fill(2025, [new LossYear(2024, 1000.01m)], Account("Revolut", [Year(2025, 5000m, 0m)]));

        Poz(declaration, 30).ShouldBe(500.00m);
    }

    [Fact]
    public void A_later_years_loss_does_not_reduce_an_earlier_year()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 1000m, 0m), Year(2026, 0m, 5000m)]));

        Poz(declaration, 30).ShouldBe(0m);
        Poz(declaration, 31).ShouldBe(1000m);
    }

    [Fact]
    public void Foreign_dividends_of_every_broker_are_in_part_G_and_those_of_Polish_companies_are_not()
    {
        var declaration = Fill(
            Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1000m, 150m)]),
            Account("XTB", [Year(2025, 0m, 0m)], dividends: [Paid(5000m, 750m), Paid(500m, 95m, Orlen)]));

        Poz(declaration, 46).ShouldBe(0m);
        Poz(declaration, 47).ShouldBe(1140.00m);
        Poz(declaration, 48).ShouldBe(900m);
        Poz(declaration, 49).ShouldBe(240m);
        Poz(declaration, 51).ShouldBe(240m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("польських компаній"));
    }

    [Fact]
    public void A_Polish_companys_dividend_on_a_foreign_broker_asks_to_check_it_was_taxed()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(500m, 0m, Orlen)]));

        Poz(declaration, 47).ShouldBe(0m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("poz. 46"));
    }

    [Fact]
    public void Poz_47_keeps_grosze_and_poz_49_is_whole_zloty()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1002.63m, 150.25m)]));

        Poz(declaration, 47).ShouldBe(190.50m);
        Poz(declaration, 48).ShouldBe(150.25m);
        Poz(declaration, 49).ShouldBe(40m);
    }

    [Fact]
    public void A_dividend_nothing_was_withheld_from_is_taxed_in_full()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(500m, 0m, Vwce)]));

        Poz(declaration, 47).ShouldBe(95m);
        Poz(declaration, 48).ShouldBe(0m);
        Poz(declaration, 49).ShouldBe(95m);
        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("без даних"));
    }

    [Fact]
    public void A_dividend_whose_withholding_is_unknown_is_flagged()
    {
        var declaration = Fill(Account("XTB", [Year(2025, 0m, 0m)], dividends: [Paid(85m, 0m, known: false)]));

        Poz(declaration, 47).ShouldBe(16.15m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("без даних про утриманий за кордоном податок (1)"));
    }

    [Fact]
    public void Tax_withheld_above_19_percent_of_a_dividend_is_credited_only_up_to_it()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1000m, 300m), Paid(1000m, 100m)]));

        Poz(declaration, 47).ShouldBe(380m);
        Poz(declaration, 48).ShouldBe(290m);
        Poz(declaration, 49).ShouldBe(90m);
    }

    [Fact]
    public void A_capital_loss_does_not_reduce_the_dividend_tax()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 1000m, 3000m)], dividends: [Paid(1000m, 150m)]));

        Poz(declaration, 35).ShouldBe(0m);
        Poz(declaration, 49).ShouldBe(40m);
        Poz(declaration, 51).ShouldBe(40m);
    }

    [Fact]
    public void Tax_withheld_over_the_treaty_rate_is_credited_up_to_19_percent_with_the_tax_offices_reading_in_a_note()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1000m, 300m)]));

        Poz(declaration, 48).ShouldBe(190m);
        Poz(declaration, 49).ShouldBe(0m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("poz. 49 = 40 zł"));
    }

    [Fact]
    public void The_treaty_rate_is_the_countrys_ten_percent_for_Japan()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1000m, 150m, Toyota)]));

        Poz(declaration, 49).ShouldBe(40m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("poz. 49 = 90 zł"));
    }

    [Fact]
    public void A_grosz_over_the_treaty_rate_is_rounding_not_a_missing_W_8BEN()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(412.37m, 61.86m)]));

        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("KIS"));
    }

    [Fact]
    public void Amounts_in_notes_are_written_as_the_form_takes_them_whatever_the_servers_culture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(10000.5m, 3000m)]));

            declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("poz. 48 = 1500,08") && Rendered(note.Short!).Contains("poz. 49 = 400 zł"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Tax_withheld_at_the_treaty_rate_needs_no_note()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 0m, 0m)], dividends: [Paid(1000m, 150m)]));

        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("KIS"));
    }

    [Fact]
    public void Entered_PIT_8C_amounts_stand_in_row_1_and_everything_below_follows_them()
    {
        var declaration = Fill(
            Account("XTB", [Year(2025, 5000m, 5400m)], pit8C: new Pit8CAmounts(5100m, 5350m)),
            Account("Revolut", [Year(2025, 10000m, 9000m)]));

        (Poz(declaration, 20), Poz(declaration, 21)).ShouldBe((5100m, 5350m));
        Poz(declaration, 28).ShouldBe(750m);
        Poz(declaration, 35).ShouldBe(143m);
        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("введіть суми з PIT-8C"));
        declaration.Accounts.Single(account => account.Broker == "XTB").Pit8C.ShouldBe(new Pit8CAmounts(5100m, 5350m));
    }

    [Fact]
    public void One_PIT_8C_covers_every_account_of_its_broker()
    {
        var declaration = Fill(
            Account("XTB", [Year(2025, 5000m, 4000m)], name: "XTB PLN", pit8C: new Pit8CAmounts(8100m, 6050m)),
            Account("XTB S.A.", [Year(2025, 3000m, 2000m)], name: "XTB USD"));

        (Poz(declaration, 20), Poz(declaration, 21)).ShouldBe((8100m, 6050m));
        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("введіть суми з PIT-8C"));
        declaration.Accounts.Single(account => account.Name == "XTB USD").Pit8CCoveredBy.ShouldBe("XTB PLN");
        declaration.Accounts.Single(account => account.Name == "XTB PLN").Pit8CCoveredBy.ShouldBeNull();
    }

    [Fact]
    public void An_earlier_years_PIT_8C_makes_the_loss_carried_from_it()
    {
        var declaration = Fill(Account("XTB", [Year(2024, 100m, 1000m), Year(2025, 5000m, 0m)],
            pit8CByYear: new Dictionary<int, Pit8CAmounts> { [2024] = new(100m, 1100m), [2025] = new(5000m, 0m) }));

        Poz(declaration, 30).ShouldBe(500m);
    }

    [Fact]
    public void Without_PIT_8C_amounts_row_1_shows_ours_and_asks_for_them()
    {
        var declaration = Fill(Account("XTB", [Year(2025, 5000m, 4000m)]));

        Poz(declaration, 20).ShouldBe(5000m);
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("введіть суми з PIT-8C"));
        declaration.Accounts.Single().TakesPit8C.ShouldBeTrue();
    }

    [Fact]
    public void PIT_ZG_is_one_per_country_of_the_instruments_sold_with_a_net_gain_in_country_order()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 5000m, 3000m)],
            sales: [Sale(Apple, 1000m), Sale(Apple, -100m), Sale(Vwce, -100m), Sale(Asml, 300m)]));

        declaration.Attachments.Select(attachment => attachment.Fields.Single(field => field.Position == 7).Text).ShouldBe(["NL", "US"]);
        var us = declaration.Attachments.Single(attachment => attachment.Title.Contains("Stany Zjednoczone"));
        us.Fields.Single(field => field.Position == 6).Text.ShouldBe("Stany Zjednoczone Ameryki");
        us.Fields.Single(field => field.Position == 29).Amount.ShouldBe(900m);
        us.Fields.Single(field => field.Position == 30).Amount.ShouldBe(0m);
        Poz(declaration, 72).ShouldBe(2m);
    }

    [Fact]
    public void Polish_papers_and_sales_reported_on_PIT_8C_need_no_PIT_ZG()
    {
        var declaration = Fill(
            Account("Revolut", [Year(2025, 2000m, 1000m)], sales: [Sale(Orlen, 1000m)]),
            Account("XTB", [Year(2025, 2000m, 1000m)], sales: [Sale(Apple, 1000m)]));

        declaration.Attachments.ShouldBeEmpty();
        Poz(declaration, 72).ShouldBe(0m);
    }

    [Fact]
    public void No_PIT_ZG_when_the_person_has_no_income_from_sales()
    {
        var declaration = Fill(
            Account("Revolut", [Year(2025, 2000m, 1000m)], sales: [Sale(Apple, 1000m)]),
            Account("XTB", [Year(2025, 1000m, 3000m)]));

        Poz(declaration, 28).ShouldBe(0m);
        declaration.Attachments.ShouldBeEmpty();
    }

    [Fact]
    public void An_international_isin_is_no_country()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)], sales: [Sale(Eurobond, 1000m)]));

        declaration.Attachments.ShouldBeEmpty();
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("XS/EU"));
    }

    [Fact]
    public void A_sale_without_an_isin_is_named_in_a_note()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)], sales: [Sale(NoIsin, 1000m)]));

        declaration.Attachments.ShouldBeEmpty();
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("без країни"));
    }

    [Fact]
    public void IKE_accounts_are_left_out_and_named()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)]), Account("XTB", [Year(2025, 9000m, 1000m)], dividends: [Paid(500m, 75m)], policy: Ike));

        Poz(declaration, 26).ShouldBe(2000m);
        Poz(declaration, 47).ShouldBe(0m);
        declaration.Accounts.Single(account => account.Broker == "XTB").Treatment.ShouldBe(Message.Of("pit38.treatment.untaxed"));
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("XTB EUR") && Rendered(note.Text).Contains("IKE"));
    }

    [Fact]
    public void Every_account_is_listed_with_how_it_is_declared_even_without_events_in_the_year()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)]), Account("XTB", [Year(2024, 100m, 50m)]));

        declaration.Accounts.Select(account => (account.Broker, Rendered(account.Treatment), account.Revenue))
            .ShouldBe([("Revolut", "Рядок 2 · дивіденди в G", 2000m), ("XTB", "Рядок 1 (PIT-8C) · дивіденди в G", 0m)]);
    }

    [Fact]
    public void The_return_is_due_on_30_April_of_the_next_year()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)]));

        declaration.Form.ShouldBe("PIT-38");
        declaration.Deadline.ShouldBe(new DateOnly(2026, 4, 30));
    }

    [Theory]
    [InlineData(2025, "2026-04-30")]
    [InlineData(2027, "2028-05-02")]
    [InlineData(2022, "2023-05-02")]
    [InlineData(2028, "2029-04-30")]
    public void A_deadline_on_a_weekend_or_holiday_moves_to_the_next_working_day(int year, string deadline) =>
        Pit38Form.Deadline(year).ShouldBe(DateOnly.Parse(deadline, System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public void Notes_point_at_PIT_8C_the_one_time_deduction_and_a_form_not_yet_published()
    {
        var declaration = Fill(2026, [new LossYear(2024, 1000m)], Account("XTB", [Year(2026, 2000m, 1000m)]));

        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("PIT-8C"));
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("5 000 000"));
        declaration.Notes.ShouldContain(note => Rendered(note.Text).Contains("2026"));
    }

    [Fact]
    public void Without_PIT_8C_brokers_or_deducted_losses_those_notes_are_absent()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)]));

        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("PIT-8C"));
        declaration.Notes.ShouldNotContain(note => Rendered(note.Text).Contains("5 000 000"));
    }

    [Fact]
    public void Totals_and_tax_are_marked_as_worked_out_by_the_online_form_and_the_rest_as_typed()
    {
        var declaration = Fill(Account("Revolut", [Year(2025, 2000m, 1000m)]));
        var fields = declaration.Sections.SelectMany(section => section.Fields).ToList();

        fields.Where(field => field.Computed).Select(field => field.Position).ShouldBe([26, 27, 28, 29, 31, 33, 35, 49, 51, 52]);
        declaration.TaxDue.ShouldBe(Poz(declaration, 51)!.Value);
    }

    [Fact]
    public void Things_to_do_before_filing_are_actions_with_a_short_line_and_explanations_are_not()
    {
        var declaration = Fill(Account("XTB", [Year(2025, 1000m, 2000m)], dividends: [Paid(85m, 0m, known: false)]));

        declaration.Notes.Where(note => note.Action).ShouldAllBe(note => note.Short != null);
        declaration.Notes.Where(note => note.Action).Select(note => Rendered(note.Short!)).ShouldContain(note => note.Contains("PIT-8C"));
        declaration.Notes.Where(note => note.Action).Select(note => Rendered(note.Short!)).ShouldContain(note => note.Contains("Збиток"));
        declaration.Notes.Where(note => note.Action).Select(note => Rendered(note.Short!)).ShouldContain(note => note.Contains("без даних"));
        declaration.Notes.Single(note => Rendered(note.Text).Contains("art. 11a")).Action.ShouldBeFalse();
        declaration.Facts!.Single().Label.ShouldBe(Message.Of("pit38.fact.foreign_dividends"));
    }

    [Fact]
    public void The_form_declares_Polish_accounts()
    {
        Form.Jurisdiction.ShouldBe(Belka.Jurisdiction);
        Form.Jurisdiction.ShouldBe(Ike.Jurisdiction);
    }

    [Theory]
    [InlineData("XTB", true)]
    [InlineData("XTB S.A.", true)]
    [InlineData("mBank", true)]
    [InlineData("mBanku", true)]
    [InlineData("Revolut", false)]
    [InlineData("Trading 212", false)]
    [InlineData("Interactive Brokers", false)]
    [InlineData("Інший", false)]
    public void Polish_brokers_are_recognised_by_whole_words(string broker, bool issues) =>
        PolishBrokers.IssuesPit8C(broker).ShouldBe(issues);

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
