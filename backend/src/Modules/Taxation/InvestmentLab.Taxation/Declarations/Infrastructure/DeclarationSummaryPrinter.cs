using System.Globalization;
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
        var sheet = new Sheet(builder, builder.AddTrueTypeFont(FormResources.Load("DejaVuSans.ttf")), builder.AddTrueTypeFont(FormResources.Load("DejaVuSans-Bold.ttf")));

        sheet.Title($"{declaration.Form} за {declaration.Year} — що куди вписати");
        sheet.Note($"Подати з 15.02 до {declaration.Deadline.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} · e-Urząd Skarbowy (podatki.gov.pl) → Twój e-PIT → {declaration.Form}. " +
            "Поля, яких тут немає, — 0 (порожні).");
        sheet.Total("До сплати", $"{Amount(declaration.TaxDue)} zł");

        foreach (var section in declaration.Sections)
            sheet.Section($"{section.Code}. {section.Title}", Shown(section.Fields));

        foreach (var attachment in declaration.Attachments)
            sheet.Section($"Załącznik {attachment.Title}", attachment.Fields);

        if (declaration.Facts is { Count: > 0 } facts)
        {
            sheet.Heading("Довідково");

            foreach (var fact in facts)
                sheet.Row(null, fact.Label, $"{Amount(fact.Amount)} zł", computed: false);
        }

        var actions = declaration.Notes.Where(note => note.Action).Select(note => note.Short ?? note.Text).ToList();
        if (actions.Count > 0)
        {
            sheet.Heading("Перед поданням");

            foreach (var action in actions)
                sheet.Bullet(action);
        }

        return builder.Build();
    }

    private static IReadOnlyList<DeclarationField> Shown(IReadOnlyList<DeclarationField> fields) =>
        [.. fields.Where(field => field.Amount is null || field.Amount != 0m)];

    private static string Amount(decimal amount) => decimal.IsInteger(amount)
        ? amount.ToString("0", CultureInfo.InvariantCulture)
        : amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Value(DeclarationField field) => field.Amount is { } amount ? Amount(amount) : field.Text ?? "";

    private sealed class Sheet(PdfDocumentBuilder builder, PdfDocumentBuilder.AddedFont regular, PdfDocumentBuilder.AddedFont bold)
    {
        private const double Width = 595.28, Height = 841.89, Margin = 48, PositionX = Margin, LabelX = Margin + 48, ValueRight = Width - Margin;
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
            const string Mark = "рахує форма — звірте";
            var valueWidth = Measure(value, bold, 11);
            var room = ValueRight - valueWidth - 16 - LabelX;
            var lines = Wrap(label, regular, 10, room);
            var markInline = computed && Measure(lines[^1], regular, 10) + 8 + Measure(Mark, regular, 8) <= room;
            var height = (lines.Count + (computed && !markInline ? 1 : 0)) * 13;
            Space(8 + height);
            _y -= 13;

            if (position is not null)
                Text(position, PositionX, 9, regular, Grey);

            Text(value, ValueRight - valueWidth, 11, bold, computed ? Grey : Ink);

            for (var index = 0; index < lines.Count; index++)
            {
                Text(lines[index], LabelX, 10, regular, Ink);

                if (index < lines.Count - 1)
                    _y -= 13;
            }

            if (computed && markInline)
                Text(Mark, LabelX + Measure(lines[^1], regular, 10) + 8, 8, regular, Grey);
            else if (computed)
            {
                _y -= 13;
                Text(Mark, LabelX, 8, regular, Grey);
            }

            _y -= 6;
            Line();
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

        private double Measure(string text, PdfDocumentBuilder.AddedFont font, double size) =>
            text.Length == 0 ? 0 : Page.MeasureText(text, size, new PdfPoint(0, 0), font) is { Count: > 0 } letters ? letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X : 0;

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
