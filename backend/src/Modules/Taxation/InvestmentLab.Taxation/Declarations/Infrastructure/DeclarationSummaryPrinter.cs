using System.Globalization;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.Taxation.Declarations.Application.Shared;
using InvestmentLab.Taxation.Declarations.Domain;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace InvestmentLab.Taxation.Declarations.Infrastructure;

public sealed class DeclarationSummaryPrinter : IDeclarationPrinter
{
    public string Kind => "summary";

    public bool Prints(TaxDeclaration declaration) => true;

    public byte[] Print(TaxDeclaration declaration)
    {
        var builder = new PdfDocumentBuilder();
        var regular = builder.AddTrueTypeFont(FormResources.Load("DejaVuSans.ttf"));
        var bold = builder.AddTrueTypeFont(FormResources.Load("DejaVuSans-Bold.ttf"));
        var sheet = new Sheet(builder, regular, bold);

        PrintHeader(sheet, declaration);
        PrintFields(sheet, declaration);
        PrintFacts(sheet, declaration);
        PrintActions(sheet, declaration);

        return builder.Build();
    }

    private static void PrintHeader(Sheet sheet, TaxDeclaration declaration)
    {
        var deadline = declaration.Deadline.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        sheet.Title(Text("tax.summary.title", new { form = declaration.Form, year = declaration.Year }));
        sheet.Note(Text("tax.summary.filing", new { deadline, form = declaration.Form }));
        sheet.Total(Text("tax.summary.total"), $"{Amount(declaration.TaxDue)} zł");
    }

    private static void PrintFields(Sheet sheet, TaxDeclaration declaration)
    {
        foreach (var section in declaration.Sections)
            sheet.Section($"{section.Code}. {section.Title}", Shown(section.Fields));

        foreach (var attachment in declaration.Attachments)
            sheet.Section($"Załącznik {attachment.Title}", attachment.Fields);
    }

    private static void PrintFacts(Sheet sheet, TaxDeclaration declaration)
    {
        if (declaration.Facts is not { Count: > 0 } facts)
            return;

        sheet.Heading(Text("tax.summary.facts"));

        foreach (var fact in facts)
            sheet.Row(null, MessageLocalizer.Current.Render(fact.Label), $"{Amount(fact.Amount)} zł", computed: false);
    }

    private static void PrintActions(Sheet sheet, TaxDeclaration declaration)
    {
        var actions = declaration.Notes.Where(note => note.Action).Select(note => note.Short ?? note.Text).ToList();
        if (actions.Count == 0)
            return;

        sheet.Heading(Text("tax.summary.before_filing"));

        foreach (var action in actions)
            sheet.Bullet(MessageLocalizer.Current.Render(action));
    }

    private static string Text(string code, object? parameters = null) => MessageLocalizer.Current.Render(code, parameters);

    private static IReadOnlyList<DeclarationField> Shown(IReadOnlyList<DeclarationField> fields) =>
        [.. fields.Where(field => field.Amount is null || field.Amount != 0m)];

    private static string Amount(decimal amount) => decimal.IsInteger(amount)
        ? amount.ToString("0", CultureInfo.InvariantCulture)
        : amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Value(DeclarationField field) => field.Amount is { } amount ? Amount(amount) : field.Text ?? "";

    private sealed class Sheet(PdfDocumentBuilder builder, PdfDocumentBuilder.AddedFont regular, PdfDocumentBuilder.AddedFont bold)
    {
        private const double Width = 595.28, Height = 841.89, Margin = 48, PositionX = Margin, LabelX = Margin + 48, ValueRight = Width - Margin;
        private const double LineHeight = 13, RowGap = 16, MarkGap = 8;
        private static string ComputedMark => MessageLocalizer.Current.Render("tax.summary.computed_mark", null);
        private static readonly (byte R, byte G, byte B) Ink = (15, 23, 42), Grey = (100, 116, 139), Rule = (226, 232, 240), Accent = (79, 70, 229);

        private PdfPageBuilder? _page;
        private double _y;

        private PdfPageBuilder Page => _page ??= NewPage();

        public void Title(string text)
        {
            Space(30);
            Text(text, LabelX - 48, 16, bold, Ink);
            _y -= 22;
        }

        public void Note(string text)
        {
            foreach (var line in Wrap(text, regular, 9, ValueRight - Margin))
            {
                Space(13);
                Text(line, Margin, 9, regular, Grey);
                _y -= 13;
            }

            _y -= 6;
        }

        public void Total(string label, string value)
        {
            _y -= 8;
            Space(34);
            Text(label, Margin, 12, regular, Ink);
            var width = Measure(value, bold, 20);
            Text(value, ValueRight - width, 20, bold, Accent);
            _y -= 12;
            Line();
            _y -= 10;
        }

        public void Heading(string text)
        {
            Space(48);
            _y -= 24;
            Text(text, Margin, 11, bold, Ink);
            _y -= 4;
        }

        public void Section(string title, IReadOnlyList<DeclarationField> fields)
        {
            if (fields.Count == 0)
                return;

            Heading(title);

            foreach (var field in fields)
                Row($"poz. {field.Position}", field.Label, Value(field), field.Computed);
        }

        public void Row(string? position, string label, string value, bool computed)
        {
            var valueWidth = Measure(value, bold, 11);
            var room = ValueRight - valueWidth - RowGap - LabelX;
            var lines = Wrap(label, regular, 10, room);
            var markInline = computed && Measure(lines[^1], regular, 10) + MarkGap + Measure(ComputedMark, regular, 8) <= room;
            var height = (lines.Count + (computed && !markInline ? 1 : 0)) * LineHeight;

            Space(8 + height);
            _y -= LineHeight;

            if (position is not null)
                Text(position, PositionX, 9, regular, Grey);

            Text(value, ValueRight - valueWidth, 11, bold, computed ? Grey : Ink);
            LabelLines(lines);

            if (computed)
                Mark(lines[^1], markInline);

            _y -= 6;
            Line();
        }

        private void LabelLines(List<string> lines)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                Text(lines[index], LabelX, 10, regular, Ink);

                if (index < lines.Count - 1)
                    _y -= LineHeight;
            }
        }

        private void Mark(string lastLine, bool inline)
        {
            if (inline)
            {
                Text(ComputedMark, LabelX + Measure(lastLine, regular, 10) + MarkGap, 8, regular, Grey);

                return;
            }

            _y -= LineHeight;
            Text(ComputedMark, LabelX, 8, regular, Grey);
        }

        public void Bullet(string text)
        {
            var lines = Wrap(text, regular, 10, ValueRight - LabelX);
            Space(6 + lines.Count * 13);
            _y -= 13;
            Text("•", Margin + 4, 10, bold, Accent);

            foreach (var (line, index) in lines.Select((line, index) => (line, index)))
            {
                Text(line, Margin + 16, 10, regular, Ink);

                if (index < lines.Count - 1)
                    _y -= 13;
            }

            _y -= 4;
        }

        private void Space(double needed)
        {
            if (_page is null || _y - needed < Margin)
                _page = NewPage();
        }

        private PdfPageBuilder NewPage()
        {
            _y = Height - Margin;

            return _page = builder.AddPage(Width, Height);
        }

        private void Text(string text, double x, double size, PdfDocumentBuilder.AddedFont font, (byte R, byte G, byte B) color)
        {
            Page.SetTextAndFillColor(color.R, color.G, color.B);
            Page.AddText(text, size, new PdfPoint(x, _y), font);
        }

        private void Line()
        {
            Page.SetStrokeColor(Rule.R, Rule.G, Rule.B);
            Page.DrawLine(new PdfPoint(Margin, _y), new PdfPoint(ValueRight, _y), 0.5);
        }

        private double Measure(string text, PdfDocumentBuilder.AddedFont font, double size)
        {
            if (text.Length == 0)
                return 0;

            var letters = Page.MeasureText(text, size, new PdfPoint(0, 0), font);

            return letters.Count > 0 ? letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X : 0;
        }

        private List<string> Wrap(string text, PdfDocumentBuilder.AddedFont font, double size, double width)
        {
            var lines = new List<string>();
            var line = "";

            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (line.Length > 0 && Measure(candidate, font, size) > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            if (line.Length > 0)
                lines.Add(line);

            return lines;
        }
    }
}
