namespace InvestmentLab.Taxation.Declarations.Domain;

public static class Pit38Notes
{
    private const int FormYear = 2025;
    private const string PolandCode = "PL";

    public static List<DeclarationNote> Of(Pit38Figures figures)
    {
        var notes = new List<DeclarationNote>
        {
            new(Message.Of("pit38.note.rates")),
            FormYearNote(figures.Input.Year),
            new(Message.Of("pit38.note.crypto")),
        };

        notes.AddRange(ConditionalNotes(figures));

        return notes;
    }

    private static DeclarationNote FormYearNote(int year)
    {
        if (year == FormYear)
            return new(Message.Of("pit38.note.form_numbers"));

        return new(
            Message.Of("pit38.note.form_numbers_old", new { year }),
            true,
            Message.Of("pit38.note.form_numbers_old_short", new { year }));
    }

    private static IEnumerable<DeclarationNote> ConditionalNotes(Pit38Figures figures)
    {
        if (MissingPit8CNote(figures) is { } missingPit8C)
            yield return missingPit8C;

        if (HasForeignDividendsAtPolishBroker(figures))
            yield return new(Message.Of("pit38.note.foreign_dividends_polish_broker"));

        if (figures.Dividends.WithholdingUnknown.Count is > 0 and var unknown)
            yield return new(Message.Of("pit38.note.withholding_unknown", new { count = unknown }), true, Message.Of("pit38.note.withholding_unknown_short", new { count = unknown }));

        if (figures.Dividends.Polish.Count > 0)
            yield return PolishDividendsNote(figures);

        if (UntaxedAccountsNote(figures) is { } untaxed)
            yield return untaxed;

        if (figures.Gains.P29 > 0m)
            yield return new(Message.Of("pit38.note.loss"), true, Message.Of("pit38.note.loss_short"));

        if (figures.Gains.P30 > 0m)
            yield return new(Message.Of("pit38.note.carryforward"));

        if (figures.Attachments.Count > 0)
            yield return new(Message.Of("pit38.note.zg"));

        if (figures.SalesWithUnknownCountry is > 0 and var withoutCountry)
            yield return new(Message.Of("pit38.note.unknown_country", new { count = withoutCountry }), true, Message.Of("pit38.note.unknown_country_short", new { count = withoutCountry }));

        if (figures.Dividends.OverTreaty.Count > 0 && figures.Dividends.KisP49 != figures.Dividends.P49)
            yield return OverTreatyNote(figures);
    }

    private static DeclarationNote? MissingPit8CNote(Pit38Figures figures)
    {
        var missing = figures.Rows.BrokersMissingPit8C();
        if (missing.Count == 0)
            return null;

        var brokers = string.Join(", ", missing);

        return new(Message.Of("pit38.note.missing_pit8c", new { brokers }), true, Message.Of("pit38.note.missing_pit8c_short", new { brokers }));
    }

    private static bool HasForeignDividendsAtPolishBroker(Pit38Figures figures) =>
        figures.Rows.WithPit8CForm.Any(account => account.Dividends.Any(dividend => figures.Input.CountryOf(dividend.InstrumentId) != PolandCode));

    private static DeclarationNote PolishDividendsNote(Pit38Figures figures)
    {
        var count = figures.Dividends.Polish.Count;
        var abroad = figures.Dividends.Polish.Any(paid => figures.Rows.WithoutPit8CForm.Contains(paid.Account));

        return abroad
            ? new(Message.Of("pit38.note.polish_dividends_abroad", new { count }), true, Message.Of("pit38.note.polish_dividends_abroad_short"))
            : new(Message.Of("pit38.note.polish_dividends", new { count }));
    }

    private static DeclarationNote? UntaxedAccountsNote(Pit38Figures figures)
    {
        var untaxed = figures.Input.Accounts.Where(account => !account.Policy.IsTaxed).Select(account => account.Name).ToList();

        return untaxed.Count == 0 ? null : new(Message.Of("pit38.note.untaxed_accounts", new { accounts = string.Join(", ", untaxed) }));
    }

    private static DeclarationNote OverTreatyNote(Pit38Figures figures)
    {
        var dividends = figures.Dividends;
        var credit = Pit38Amounts.Typed(dividends.KisCredit);
        var difference = Pit38Amounts.Typed(dividends.KisP49);

        return new(
            Message.Of("pit38.note.over_treaty", new { count = dividends.OverTreaty.Count, credit, difference }),
            true,
            Message.Of("pit38.note.over_treaty_short", new { credit, difference }));
    }
}
