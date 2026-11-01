using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Declarations.Infrastructure;
using UglyToad.PdfPig;

namespace InvestmentLab.Taxation.Tests;

public sealed class Pit38PdfPrinterTests
{
    private static readonly Guid Apple = Guid.NewGuid(), Asml = Guid.NewGuid();

    private static TaxDeclaration Declaration() => new Pit38Form().Fill(new DeclarationFacts(2025, [
        new DeclarationAccount("Revolut EUR", "Revolut", new BelkaTaxPolicy(),
            [new TaxYearSummary(2025, 8385.00m, 6895.05m, 1489.95m, 0m, 0m, 0m, 1002.63m, 0m, 0m, 0m, 150.25m)],
            [Sale(Apple, 1200m), Sale(Asml, 289.95m)],
            [new DividendIncome(Guid.NewGuid(), new DateOnly(2025, 5, 1), Apple, 852.38m, 852.38m, 1m, 1002.63m, 150.25m, WithholdingKnown: true)]),
        new DeclarationAccount("XTB PLN", "XTB", new BelkaTaxPolicy(), [new TaxYearSummary(2025, 12000m, 11500.50m, 499.50m, 0m, 0m, 0m, 0m, 0m, 0m, 0m)], [], []),
    ], [], id => id == Apple ? "US" : id == Asml ? "NL" : null));

    private static Realization Sale(Guid instrument, decimal gain) =>
        new(Guid.NewGuid(), new DateOnly(2025, 6, 1), instrument, 1m, 0m, 0m, gain, 4m, null, [], [], 0m);

    [Fact]
    public void Prints_the_three_pages_of_PIT_38_and_two_of_each_PIT_ZG_with_the_amounts_in_their_boxes()
    {
        var declaration = Declaration();
        var pdf = new Pit38PdfPrinter().Print(declaration);

        if (Environment.GetEnvironmentVariable("PIT38_SAMPLE") is { Length: > 0 } sample)
            File.WriteAllBytes(sample, pdf);

        using var document = PdfDocument.Open(pdf);
        document.NumberOfPages.ShouldBe(3 + 2 * declaration.Attachments.Count);
        declaration.Attachments.Count.ShouldBe(2);

        var written = document.GetPages().SelectMany(page => page.Letters.Where(letter => (letter.FontName ?? "").Contains("DejaVu")).Select(letter => (page.Number, letter))).ToList();
        string Line(int page, double baseline, double from, double to) => string.Concat(written
            .Where(w => w.Number == page && Math.Abs(w.letter.StartBaseLine.Y - baseline) < 0.5 && w.letter.StartBaseLine.X >= from && w.letter.StartBaseLine.X < to)
            .OrderBy(w => w.letter.StartBaseLine.X).Select(w => w.letter.Value));

        Line(1, 284.1, 139, 249).ShouldBe("1200000");
        Line(1, 258.6, 139, 249).ShouldBe("838500");
        Line(1, 258.6, 249, 359).ShouldBe("689505");
        Line(1, 200.8, 359, 468).ShouldBe("198945");
        Line(1, 125.3, 465, 570).ShouldBe("1989");
        Line(1, 100.3, 124, 210).ShouldBe("1900");
        written.Where(w => w.Number == 1 && Math.Abs(w.letter.StartBaseLine.Y - 100.3) < 0.5 && w.letter.StartBaseLine.X is > 124 and < 210)
            .Select(w => w.letter.StartBaseLine.X).OrderBy(x => x).Skip(2).First().ShouldBeGreaterThan(187.5);
        Line(1, 48.8, 465, 570).ShouldBe("378");
        Line(1, 699.5, 285, 360).ShouldBe("2025");
        Line(2, 497.7, 465, 570).ShouldBe("19050");
        Line(2, 398.1, 465, 570).ShouldBe("41800");
        Line(3, 717, 25, 200).ShouldBe("2");
        Line(4, 545, 50, 400).ShouldBe("Holandia");
        Line(6, 545, 50, 400).ShouldBe("Stany Zjednoczone Ameryki");
        Line(6, 173.4, 371, 570).ShouldBe("120000");
        Line(6, 545.5, 490, 530).ShouldBe("US");
        Line(1, 507.5, 70, 90).ShouldBe("X");

        written.Where(w => w.Number == 2 && Math.Abs(w.letter.StartBaseLine.Y - 497.7) < 0.5 && w.letter.StartBaseLine.X < 546).Max(w => w.letter.EndBaseLine.X).ShouldBeLessThan(542);

        var zloty = written.Where(w => w.Number == 1 && Math.Abs(w.letter.StartBaseLine.Y - 258.6) < 0.5 && w.letter.StartBaseLine.X < 222.9).ToList();
        zloty.Max(w => w.letter.EndBaseLine.X).ShouldBeLessThan(222.9);
    }

    [Fact]
    public void Every_position_PIT_38_fills_has_a_box_on_the_form()
    {
        var declaration = Declaration();

        declaration.Sections.SelectMany(section => section.Fields).Select(field => field.Position).ShouldAllBe(position => Pit38PdfPrinter.Pit38.ContainsKey(position));
        declaration.Attachments.SelectMany(attachment => attachment.Fields).Select(field => field.Position).ShouldAllBe(position => Pit38PdfPrinter.PitZg.ContainsKey(position));
    }
}
