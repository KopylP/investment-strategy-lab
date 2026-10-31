using InvestmentLab.Taxation.BrokerStatements.Domain;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace InvestmentLab.Taxation.BrokerStatements.Infrastructure;

internal sealed class RevolutPnlPdfParser : IBrokerStatementParser
{
    private const double LineTolerance = 2.0;

    public string Broker => "Revolut";

    public Result<ParsedBrokerStatement> Parse(byte[] content)
    {
        List<IReadOnlyList<string>> lines = [];

        try
        {
            using var document = PdfDocument.Open(content);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords(NearestNeighbourWordExtractor.Instance).Where(word => !string.IsNullOrWhiteSpace(word.Text))
                    .OrderByDescending(word => word.BoundingBox.Bottom).ToList();
                var line = new List<UglyToad.PdfPig.Content.Word>();

                foreach (var word in words)
                {
                    if (line.Count > 0 && Math.Abs(line[0].BoundingBox.Bottom - word.BoundingBox.Bottom) > LineTolerance)
                    {
                        lines.Add([.. line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)]);
                        line.Clear();
                    }

                    line.Add(word);
                }

                if (line.Count > 0)
                    lines.Add([.. line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)]);

                lines.Add([]);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return BrokerStatementErrors.Unreadable(exception.Message);
        }

        return PnlStatementReader.Read(Broker, lines);
    }
}
