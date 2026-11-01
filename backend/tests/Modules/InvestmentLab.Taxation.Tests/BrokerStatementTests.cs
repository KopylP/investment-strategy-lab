using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class BrokerStatementTests
{
    private static readonly Guid Apple = Guid.NewGuid(), Sony = Guid.NewGuid(), Spinoff = Guid.NewGuid(), Eu = Guid.NewGuid();
    private static readonly BelkaTaxPolicy Belka = new();

    private static IReadOnlyList<IReadOnlyList<string>> Lines(params string[] lines) =>
        [.. lines.Select(line => (IReadOnlyList<string>)(line.Length == 0 ? [] : line.Split(' ')))];

    [Theory]
    [InlineData("\u00A0")]
    [InlineData("\u202F")]
    public void Amounts_grouped_with_a_no_break_space_are_read_as_one_number(string space)
    {
        var amount = $"\u20AC1{space}234,56";

        var statement = PnlStatementReader.Read("Revolut", Lines(
            $"2025-05-06 ASME ASML NL0010273215 NL {amount} \u20AC0,00 {amount}",
            $"1{space}234,56 PLN 0,00 PLN"));

        var dividend = statement.Dividends.ShouldHaveSingleItem();
        dividend.Gross.ShouldBe(1234.56m);
        dividend.GrossInTaxCurrency.ShouldBe(1234.56m);
    }

    [Fact]
    public void A_row_with_no_amount_in_its_own_currency_is_reported_as_unreadable()
    {
        var statement = PnlStatementReader.Read("Revolut", Lines(
            "2025-05-06 ASME ASML NL0010273215 NL",
            "1,50 PLN 0,00 PLN"));

        statement.Dividends.ShouldBeEmpty();
        statement.Warnings.ShouldHaveSingleItem().ShouldContain("tax.broker.unreadable_dividend");
    }

    [Fact]
    public void Rows_are_read_by_their_shape_whatever_the_headings_say()
    {
        var statement = PnlStatementReader.Read("Revolut", Lines(
            "Sprzedaż",
            "Data nabycia Data sprzedaży Symbol Nazwa ISIN Kraj Ilość Koszt Przychód Zysk Opłaty",
            "2025-04-23 2025-07-07 TM Toyota US8923313071 US 0.35583561 US$64.94 US$59.67 -US$5.27 US$1.07",
            "245.70 PLN 216 PLN -29.70 PLN 4.04 PLN",
            "Kurs: 3.784 Kurs: 3.62",
            "2025-10-08 2025-11-20 SFGYY Sony Financial US83571B1008 US 0.56779242 US$2.27 US$2.56 US$0.29 US$1.09",
            "Group",
            "8.30 PLN 9.39 PLN 1.09 PLN 4 PLN",
            "Razem US$67.21 US$62.23 -US$4.98 US$2.16",
            "1,486.56 PLN 1,612.52 PLN 125.96 PLN 28.11 PLN",
            "",
            "2025-05-06 ASME ASML Holding N.V. dividend NL0010273215 NL €0.08 €0.01 €0.07",
            "0.34 PLN 0.04 PLN 0.29 PLN",
            "2025-06-01 Interest on cash €0.50 €0 €0.50"));

        statement.Lots.Count.ShouldBe(2);
        statement.Lots[0].ShouldBe(new BrokerSaleLot(new(2025, 4, 23), new(2025, 7, 7), "TM", "US8923313071", "USD", 0.35583561m, 64.94m, 59.67m, 1.07m, 245.70m, 216m, 4.04m));
        statement.Lots[1].CostInTaxCurrency.ShouldBe(8.30m);
        statement.Dividends.ShouldHaveSingleItem().ShouldBe(
            new BrokerDividend(new(2025, 5, 6), "ASME", "NL0010273215", "EUR", 0.08m, 0.01m, 0.07m, 0.34m, 0.04m));
        statement.Warnings.ShouldHaveSingleItem().ShouldContain("ISIN");
    }

    [Fact]
    public void Sales_match_by_day_and_quantity_and_dividends_by_day_and_isin()
    {
        TaxEvent sale = new(Guid.NewGuid(), new(2025, 11, 20), 1, TaxableKind.Sell, Spinoff, 0.56779242m, 4.51m, 1.09m, 2.56m, [], 4m, null);
        TaxEvent apple = new(Guid.NewGuid(), new(2025, 5, 16), 2, TaxableKind.Dividend, Apple, 0m, 0m, 0m, 85m, [], 4m, null);
        TaxEvent sony = new(Guid.NewGuid(), new(2025, 5, 16), 3, TaxableKind.Dividend, Sony, 0m, 0m, 0m, 40m, [], 4m, null);

        var match = BrokerFacts.Match(
            [sale, apple, sony],
            [new(new(2025, 10, 8), new(2025, 11, 20), "SFGYY", "US83571B1008", "USD", 0.56779242m, 2.27m, 2.56m, 1.09m, 8.30m, 9.39m, 4m)],
            [new(new(2025, 5, 16), "SONY", "US8356993076", "USD", 50m, 10m, 40m, 200m, 40m), new(new(2025, 5, 16), "AAPL", "US0378331005", "USD", 100m, 15m, 85m, 400m, 60m)],
            new Dictionary<Guid, InstrumentKey> { [Apple] = new("US0378331005", ["AAPL"]), [Sony] = new(null, ["SONY"]) });

        match.Sales[sale.TransactionId].Lots.ShouldHaveSingleItem().AcquiredOn.ShouldBe(new DateOnly(2025, 10, 8));
        match.Sales[sale.TransactionId].Cost.ShouldBe(2.27m);
        match.Sales[sale.TransactionId].GainInTaxCurrency.ShouldBe(9.39m - 8.30m - 4m);
        match.Dividends[apple.TransactionId].ShouldBe(new WithholdingFacts(100m / 85m, 15m / 85m));
        match.Dividends[sony.TransactionId].ShouldBe(new WithholdingFacts(50m / 40m, 10m / 40m));
    }

    [Fact]
    public void Dividend_tax_is_on_gross_less_the_tax_withheld_abroad()
    {
        var computation = TaxCalculator.Compute([
            new TaxEvent(Guid.NewGuid(), new(2025, 5, 16), 0, TaxableKind.Dividend, Apple, 0m, 0m, 0m, 85m, [new(Eu, 0m)], 4m, null,
                Withholding: new WithholdingFacts(100m / 85m, 15m / 85m)),
        ], Belka, []);

        var dividend = computation.Year(2025)!.Dividends.ShouldHaveSingleItem();
        dividend.GrossInTaxCurrency.ShouldBe(400m);
        dividend.WithheldInTaxCurrency.ShouldBe(60m);
        dividend.WithholdingKnown.ShouldBeTrue();
        var year = computation.Year(2025)!.Summary;
        year.DividendIncome.ShouldBe(400m);
        year.DividendTaxCredit.ShouldBe(60m);
        year.DividendTax.ShouldBe(16m);
    }

    [Fact]
    public void Shares_the_journal_never_bought_take_their_cost_from_the_broker()
    {
        var computation = TaxCalculator.Compute([
            new TaxEvent(Guid.NewGuid(), new(2025, 11, 20), 0, TaxableKind.Sell, Spinoff, 0.56779242m, 4.51m, 1.09m, 0.56779242m * 4.51m, [new(Eu, 0.56779242m)], 3.67m, null,
                Broker: new SaleFacts([new BrokerLotFact(new(2025, 10, 8), 0.56779242m, 2.27m, 8.30m)], 1.09m, "USD", 2.27m)),
        ], Belka, []);

        var sale = computation.Year(2025)!.Realizations.ShouldHaveSingleItem();
        sale.UnmatchedQuantity.ShouldBe(0m);
        sale.Lots.ShouldHaveSingleItem().ShouldBe(new LotMatch(Guid.Empty, new(2025, 10, 8), 0.56779242m, 8.30m, FromBroker: true, Gross: 2.27m));
        sale.CostInTaxCurrency.ShouldBe(8.30m + 4.00m);
        sale.BrokerGainInTaxCurrency.ShouldBe(1.09m);
        computation.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void A_usd_trade_on_a_eur_account_is_taxed_in_usd()
    {
        var recorded = new TransactionRecordedV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Buy", new(2025, 4, 22), "Import", null, "EUR",
            new TradeRecordedDto(Apple, "Buy", 0.15214042m, 457.27m, 0m, [new(Eu, 0.15214042m)], SettledAmount: 80m, SettledCurrency: "USD"), null, null);

        var entry = LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!;

        entry.Currency.ShouldBe("USD");
        (entry.Quantity * entry.Price).ShouldBe(80m, 0.0001m);
    }

    [Fact]
    public void A_cost_the_broker_carved_out_for_a_spin_off_is_taken_at_our_rate()
    {
        TaxEvent buy = new(Guid.NewGuid(), new(2025, 4, 22), 0, TaxableKind.Buy, Sony, 2.8389621m, 24.28m, 1.06m, 68.93m, [new(Eu, 2.8389621m)], 3.7661m, null);
        TaxEvent sell = new(Guid.NewGuid(), new(2026, 1, 26), 1, TaxableKind.Sell, Sony, 2.8389621m, 23.10m, 0.01m, 65.58m, [new(Eu, 2.8389621m)], 3.55m, null,
            Broker: new SaleFacts([new BrokerLotFact(new(2025, 4, 22), 2.8389621m, 66.66m, 251.05m)], -23.39m, "USD", 66.66m));

        var sale = TaxCalculator.Compute([buy, sell], Belka, []).Year(2026)!.Realizations.ShouldHaveSingleItem();

        var lot = sale.Lots.ShouldHaveSingleItem();
        lot.FromBroker.ShouldBeTrue();
        lot.CostInTaxCurrency.ShouldBe(decimal.Round(2.8389621m * 24.28m * 3.7661m, 2) + decimal.Round(1.06m * 3.7661m, 2) + decimal.Round((66.66m - 2.8389621m * 24.28m) * 3.7661m, 2));
    }
}
