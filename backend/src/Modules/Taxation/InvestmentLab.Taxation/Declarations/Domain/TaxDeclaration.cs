using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

/// <summary>
/// A yearly tax return a form fills from the person's accounts. Forms are optional add-ons to tax policies: each one
/// declares the accounts whose policy belongs to its jurisdiction, so a new country is a new policy and a new form.
/// </summary>
public interface ITaxDeclarationForm
{
    /// <summary>The form's code, also its address in the API ("PIT-38").</summary>
    string Code { get; }

    string Title { get; }

    /// <summary>The <see cref="ITaxPolicy.Jurisdiction"/> of the accounts it declares.</summary>
    string Jurisdiction { get; }

    TaxDeclaration Fill(DeclarationInput input);
}

/// <summary>
/// One account's tax years; <paramref name="Sales"/> and <paramref name="Dividends"/> are those of the declared year,
/// <paramref name="Pit8C"/> what its broker's PIT-8C says of the year, once the person entered it.
/// </summary>
public sealed record DeclarationAccount(
    string Name, string Broker, ITaxPolicy Policy, IReadOnlyList<TaxYearSummary> Years, IReadOnlyList<Realization> Sales, IReadOnlyList<DividendIncome> Dividends,
    Guid AccountId = default, Pit8CAmounts? Pit8C = null)
{
    public TaxYearSummary? In(int year) => Years.FirstOrDefault(summary => summary.Year == year);
}

/// <param name="EarlierLosses">Losses from before the journal the person entered.</param>
/// <param name="CountryOf">Country code of an instrument (its ISIN's first two letters), null when unknown.</param>
public sealed record DeclarationInput(int Year, IReadOnlyList<DeclarationAccount> Accounts, IReadOnlyList<LossYear> EarlierLosses, Func<Guid, string?> CountryOf);

/// <param name="Position">The position number on the form ("poz.").</param>
/// <param name="Amount">An amount in the form's currency; null for a text field (<paramref name="Text"/>).</param>
/// <param name="Hint">Where the value comes from, in the app's language.</param>
public sealed record DeclarationField(int Position, string Label, decimal? Amount, string? Text = null, string? Hint = null);

public sealed record DeclarationSection(string Code, string Title, IReadOnlyList<DeclarationField> Fields);

public sealed record DeclarationAttachment(string Form, string Title, IReadOnlyList<DeclarationField> Fields);

/// <summary>Revenue (poz. 35) and costs (poz. 36) of a year on a broker's PIT-8C.</summary>
public sealed record Pit8CAmounts(decimal Revenue, decimal Costs);

/// <param name="Treatment">How the form takes the account, in the app's language.</param>
/// <param name="TakesPit8C">The account's sales go to row 1, where its broker's PIT-8C amounts belong.</param>
public sealed record DeclarationAccountLine(
    string Name, string Broker, string Treatment, decimal Revenue, decimal Costs, decimal DividendsGross, decimal DividendsWithheld,
    Guid AccountId = default, bool TakesPit8C = false, Pit8CAmounts? Pit8C = null);

public sealed record TaxDeclaration(
    string Form,
    string Title,
    int Year,
    DateOnly Deadline,
    IReadOnlyList<DeclarationSection> Sections,
    IReadOnlyList<DeclarationAttachment> Attachments,
    IReadOnlyList<DeclarationAccountLine> Accounts,
    IReadOnlyList<string> Notes);
