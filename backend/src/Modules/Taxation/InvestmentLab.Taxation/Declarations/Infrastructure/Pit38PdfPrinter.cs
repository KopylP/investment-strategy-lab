using System.Globalization;
using InvestmentLab.Taxation.Declarations.Application;
using InvestmentLab.Taxation.Declarations.Domain;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace InvestmentLab.Taxation.Declarations.Infrastructure;

/// <summary>
/// PIT-38 on the Ministry's own form, PIT-38(18), followed by one PIT/ZG(8) per country: the official PDFs (not
/// fillable) with the amounts written into their boxes. Personal data (parts A, B) is left for the taxpayer. The box
/// positions were measured on these exact files: złoty end at the comma printed in the box, grosze follow it.
/// </summary>
public sealed class Pit38PdfPrinter : IDeclarationPrinter
{
    private const double FontSize = 9;

    /// <summary>Points from the end of the złoty to the printed comma, and from the comma to the grosze.</summary>
    private const double BeforeComma = 2, AfterComma = 4;

    /// <summary>Parts D and G print "zł" just before the comma, from x 542: the złoty end before it.</summary>
    private const double InPrintedZl = 540;

    /// <summary>Box 1 of poz. 6, "złożenie zeznania" (a correction is ticked by hand).</summary>
    private static readonly PdfPoint FilingBox = new(75.6, 507.5);

    public string Kind => "form";

    public bool Prints(TaxDeclaration declaration) => declaration.Form == "PIT-38";

    /// <summary>Positions of PIT-38(18) by "poz." number; the year is position 4.</summary>
    public static readonly IReadOnlyDictionary<int, Box> Pit38 = new Dictionary<int, Box>
    {
        [4] = new Cells(1, [290.5, 305.0, 321.2, 337.4, 353.6], 699.5),
        [20] = new Amount(1, 223.0, 284.1),
        [21] = new Amount(1, 332.9, 284.1),
        [22] = new Amount(1, 222.9, 258.6),
        [23] = new Amount(1, 332.4, 258.6),
        [24] = new Amount(1, 222.5, 226.1),
        [25] = new Amount(1, 332.5, 226.1),
        [26] = new Amount(1, 221.9, 200.8),
        [27] = new Amount(1, 332.4, 200.8),
        [28] = new Amount(1, 441.7, 200.8),
        [29] = new Amount(1, 552.7, 200.8),
        [30] = new Amount(1, 546.5, 151.8, ZlotyEnd: InPrintedZl),
        [31] = new Whole(1, 564, 125.3),
        [32] = new Amount(1, 187.5, 100.3),
        [33] = new Amount(1, 546.5, 100.8, ZlotyEnd: InPrintedZl),
        [34] = new Amount(1, 546.5, 75.2, ZlotyEnd: InPrintedZl),
        [35] = new Whole(1, 564, 48.8),
        [46] = new Amount(2, 546.2, 523.4, ZlotyEnd: InPrintedZl),
        [47] = new Amount(2, 546.2, 497.7, ZlotyEnd: InPrintedZl),
        [48] = new Amount(2, 546.2, 472.3, ZlotyEnd: InPrintedZl),
        [49] = new Amount(2, 546.2, 446.7, ZlotyEnd: InPrintedZl),
        [50] = new Whole(2, 564, 420.6),
        [51] = new Amount(2, 546.2, 398.1, ZlotyEnd: InPrintedZl),
        [52] = new Amount(2, 546.2, 374.1, ZlotyEnd: InPrintedZl),
        [72] = new Text(3, 60, 717),
    };

    /// <summary>Positions of PIT/ZG(8); the year is position 2.</summary>
    public static readonly IReadOnlyDictionary<int, Box> PitZg = new Dictionary<int, Box>
    {
        [2] = new Cells(1, [278.0, 294.2, 310.4, 326.6, 342.8], 700),
        [6] = new Text(1, 58, 545),
        [7] = new Cells(1, [494.9, 511.1, 527.3], 545.5),
        [29] = new Amount(1, 541.4, 173.4),
        [30] = new Amount(1, 541.4, 149.4),
    };

    public byte[] Print(TaxDeclaration declaration)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddTrueTypeFont(Resource("DejaVuSans.ttf"));
        using var pit38 = PdfDocument.Open(Resource("PIT-38(18).pdf"));
        using var pitZg = PdfDocument.Open(Resource("PIT-ZG(8).pdf"));

        var pages = Fill(builder, font, pit38, Pit38, 4, declaration.Year, declaration.Sections.SelectMany(section => section.Fields));
        pages[0].AddText("X", 11, FilingBox, font);
        foreach (var attachment in declaration.Attachments.Where(attachment => attachment.Form == "PIT/ZG"))
            Fill(builder, font, pitZg, PitZg, 2, declaration.Year, attachment.Fields);
        return builder.Build();
    }

    private static IReadOnlyList<PdfPageBuilder> Fill(PdfDocumentBuilder builder, PdfDocumentBuilder.AddedFont font, PdfDocument form, IReadOnlyDictionary<int, Box> layout, int yearPosition, int year,
        IEnumerable<DeclarationField> fields)
    {
        var pages = Enumerable.Range(1, form.NumberOfPages).Select(number => builder.AddPage(form, number)).ToList();
        Write(pages, font, layout[yearPosition], year.ToString(CultureInfo.InvariantCulture));
        foreach (var field in fields)
        {
            if (!layout.TryGetValue(field.Position, out var box))
                throw new InvalidOperationException($"The form has no box for poz. {field.Position}.");
            Write(pages, font, box, field.Amount is { } amount ? Format(amount, box) : field.Text ?? "");
        }
        return pages;
    }

    private static void Write(IReadOnlyList<PdfPageBuilder> pages, PdfDocumentBuilder.AddedFont font, Box box, string value)
    {
        var page = pages[box.Page - 1];
        double Width(string text) => text.Length == 0 ? 0 : page.MeasureText(text, FontSize, new PdfPoint(0, 0), font) is var letters && letters.Count > 0
            ? letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X
            : 0;

        switch (box)
        {
            case Amount amount:
                var (zloty, grosze) = (value.Split(',')[0], value.Split(',')[1]);
                page.AddText(zloty, FontSize, new PdfPoint((amount.ZlotyEnd ?? amount.Comma - BeforeComma) - Width(zloty), amount.Baseline), font);
                page.AddText(grosze, FontSize, new PdfPoint(amount.Comma + AfterComma, amount.Baseline), font);
                break;
            case Whole whole:
                page.AddText(value, FontSize, new PdfPoint(whole.Right - Width(value), whole.Baseline), font);
                break;
            case Cells cells:
                for (var index = 0; index < value.Length && index + 1 < cells.Edges.Count; index++)
                {
                    var character = value[index].ToString();
                    var middle = (cells.Edges[index] + cells.Edges[index + 1]) / 2 + 1.8;
                    page.AddText(character, FontSize, new PdfPoint(middle - Width(character) / 2, cells.Baseline), font);
                }
                break;
            case Text text:
                page.AddText(value, FontSize, new PdfPoint(text.Left, text.Baseline), font);
                break;
        }
    }

    /// <summary>"8385,00" for a box with grosze, "1490" for a whole-złoty box (the form prints "zł" after it).</summary>
    private static string Format(decimal amount, Box box) => box is Amount
        ? Math.Abs(amount).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',')
        : decimal.Round(Math.Abs(amount), 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

    private static byte[] Resource(string name) => FormResources.Load(name);

    /// <summary>Where a value goes on a page (1-based), in points from the page's bottom left.</summary>
    public abstract record Box(int Page);

    /// <summary>A "zł, gr" box: <paramref name="Comma"/> is where the form prints the comma; <paramref name="ZlotyEnd"/> where the złoty must end when "zł" is printed before it.</summary>
    public sealed record Amount(int Page, double Comma, double Baseline, double? ZlotyEnd = null) : Box(Page);

    /// <summary>A whole-złoty box, right-aligned before the printed "zł".</summary>
    public sealed record Whole(int Page, double Right, double Baseline) : Box(Page);

    /// <summary>One character per printed cell; <paramref name="Edges"/> are the cell borders.</summary>
    public sealed record Cells(int Page, IReadOnlyList<double> Edges, double Baseline) : Box(Page);

    public sealed record Text(int Page, double Left, double Baseline) : Box(Page);
}
