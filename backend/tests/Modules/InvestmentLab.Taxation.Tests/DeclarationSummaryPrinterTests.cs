using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Declarations.Infrastructure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace InvestmentLab.Taxation.Tests;

public sealed class DeclarationSummaryPrinterTests
{
    private static readonly Guid Apple = Guid.NewGuid();

    private static TaxDeclaration Declaration() => new Pit38Form().Fill(new DeclarationInput(2025, [
        new DeclarationAccount("Revolut EUR", "Revolut", new BelkaTaxPolicy(),
            [new TaxYearSummary(2025, 8385.00m, 6895.05m, 1489.95m, 0m, 0m, 0m, 1002.63m, 0m, 0m, 0m, 150.25m)],
            [new Realization(Guid.NewGuid(), new DateOnly(2025, 6, 1), Apple, 1m, 0m, 0m, 1489.95m, 4m, null, [], [], 0m)],
            [new DividendIncome(Guid.NewGuid(), new DateOnly(2025, 5, 1), Apple, 852.38m, 852.38m, 1m, 1002.63m, 150.25m, WithholdingKnown: true)]),
        new DeclarationAccount("XTB PLN", "XTB", new BelkaTaxPolicy(), [new TaxYearSummary(2025, 12000m, 11500.50m, 499.50m, 0m, 0m, 0m, 0m, 0m, 0m, 0m)], [], []),
    ], [], id => id == Apple ? "US" : null));

    private static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
    }

    [Fact]
    public void Lists_the_tax_due_and_every_amount_to_type_or_check_in_the_forms_order_without_the_zeros()
    {
        var declaration = Declaration();
        var pdf = new DeclarationSummaryPrinter().Print(declaration);
        if (Environment.GetEnvironmentVariable("PIT38_SUMMARY_SAMPLE") is { Length: > 0 } sample)
            File.WriteAllBytes(sample, pdf);
        var text = Text(pdf);

        text.ShouldContain("PIT-38 за 2025");
        text.ShouldContain("418 zł");                          // poz. 51: 378 + 40
        foreach (var (position, value) in new[] { (20, "12000"), (21, "11500,50"), (22, "8385"), (23, "6895,05"), (26, "20385"), (27, "18395,55"), (28, "1989,45"),
                     (31, "1989"), (33, "377,91"), (35, "378"), (47, "190,50"), (48, "150,25"), (49, "40"), (51, "418"), (29, "1489,95") })
        {
            text.ShouldContain($"poz. {position}");
            text.ShouldContain(value);
        }
        text.ShouldNotContain("poz. 24");                       // zeros stay out: an empty box is 0
        text.ShouldNotContain("poz. 46");
        text.ShouldContain("рахує форма");
        text.ShouldContain("Stany Zjednoczone Ameryki");       // PIT/ZG
        text.ShouldContain("Дивіденди з-за кордону брутто");
        text.ShouldContain("1002,63");
        text.ShouldContain("Перед поданням");
        text.ShouldContain("PIT-8C");                          // XTB without its PIT-8C amounts yet
        text.ShouldNotContain("art. 11a");                     // explanations are left for the app
    }

    [Fact]
    public void Prints_any_form_and_runs_onto_more_pages_when_long()
    {
        var many = Enumerable.Range(1, 80).Select(position => new DeclarationField(position, $"Pole numer {position} z dość długą nazwą, która się zawija w dwie linie tekstu", position * 10.5m)).ToList();
        var declaration = new TaxDeclaration("XYZ-1", "Test", 2025, new DateOnly(2026, 4, 30), [new DeclarationSection("A", "Wszystko", many)], [], [], [], 1m);

        using var document = PdfDocument.Open(new DeclarationSummaryPrinter().Print(declaration));

        document.NumberOfPages.ShouldBeGreaterThan(1);
        string.Concat(document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))).ShouldContain("poz. 80");
    }
}
