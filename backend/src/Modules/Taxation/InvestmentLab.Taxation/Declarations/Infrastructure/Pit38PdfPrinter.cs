using System.Globalization;
using InvestmentLab.Taxation.Declarations.Application.Shared;
using InvestmentLab.Taxation.Declarations.Domain;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace InvestmentLab.Taxation.Declarations.Infrastructure;

public sealed class Pit38PdfPrinter : IDeclarationPrinter
{
    private const double FontSize = 9;

    private const double BeforeComma = 2, AfterComma = 4;

    private const double InPrintedZl = 540;

    private const double CellShift = 1.8;

    private static readonly PdfPoint FilingBox = new(75.6, 507.5);

    public string Kind => "form";

    public bool Prints(TaxDeclaration declaration) => declaration.Form == "PIT-38";

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

    private static IReadOnlyList<PdfPageBuilder> Fill(
        PdfDocumentBuilder builder,
        PdfDocumentBuilder.AddedFont font,
        PdfDocument form,
        IReadOnlyDictionary<int, Box> layout,
        int yearPosition,
        int year,
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

        switch (box)
        {
            case Amount amount:
                WriteAmount(page, font, amount, value);
                break;
            case Whole whole:
                page.AddText(value, FontSize, new PdfPoint(whole.Right - WidthOf(page, font, value), whole.Baseline), font);
                break;
            case Cells cells:
                WriteCells(page, font, cells, value);
                break;
            case Text text:
                page.AddText(value, FontSize, new PdfPoint(text.Left, text.Baseline), font);
                break;
        }
    }

    private static void WriteAmount(PdfPageBuilder page, PdfDocumentBuilder.AddedFont font, Amount amount, string value)
    {
        var parts = value.Split(',');
        var (zloty, grosze) = (parts[0], parts[1]);
        var zlotyEnd = amount.ZlotyEnd ?? amount.Comma - BeforeComma;

        page.AddText(zloty, FontSize, new PdfPoint(zlotyEnd - WidthOf(page, font, zloty), amount.Baseline), font);
        page.AddText(grosze, FontSize, new PdfPoint(amount.Comma + AfterComma, amount.Baseline), font);
    }

    private static void WriteCells(PdfPageBuilder page, PdfDocumentBuilder.AddedFont font, Cells cells, string value)
    {
        for (var index = 0; index < value.Length && index + 1 < cells.Edges.Count; index++)
        {
            var character = value[index].ToString();
            var middle = (cells.Edges[index] + cells.Edges[index + 1]) / 2 + CellShift;

            page.AddText(character, FontSize, new PdfPoint(middle - WidthOf(page, font, character) / 2, cells.Baseline), font);
        }
    }

    private static double WidthOf(PdfPageBuilder page, PdfDocumentBuilder.AddedFont font, string text)
    {
        if (text.Length == 0)
            return 0;

        var letters = page.MeasureText(text, FontSize, new PdfPoint(0, 0), font);

        return letters.Count > 0 ? letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X : 0;
    }

    private static string Format(decimal amount, Box box) => box is Amount
        ? Math.Abs(amount).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',')
        : decimal.Round(Math.Abs(amount), 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

    private static byte[] Resource(string name) => FormResources.Load(name);

    public abstract record Box(int Page);

    public sealed record Amount(int Page, double Comma, double Baseline, double? ZlotyEnd = null) : Box(Page);

    public sealed record Whole(int Page, double Right, double Baseline) : Box(Page);

    public sealed record Cells(int Page, IReadOnlyList<double> Edges, double Baseline) : Box(Page);

    public sealed record Text(int Page, double Left, double Baseline) : Box(Page);
}
