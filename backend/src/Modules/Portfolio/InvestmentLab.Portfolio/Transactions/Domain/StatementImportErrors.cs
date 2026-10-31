using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class StatementImportErrors
{
    public static Error NotFound(Guid importId) => Error.NotFound("statement_import.not_found", $"Import {importId} does not exist.");

    public static Error NotInPreview(Guid importId) =>
        Error.Conflict("statement_import.not_in_preview", $"Import {importId} is already confirmed or undone.");

    public static Error NotConfirmed(Guid importId) => Error.Conflict("statement_import.not_confirmed", $"Import {importId} was not confirmed.");

    public static Error UnknownFormat(string details) =>
        Error.Validation("statement_import.unknown_format", $"The file is not a Revolut statement: {details}");

    public static Error Unmapped(IEnumerable<string> tickers) =>
        Error.Validation("statement_import.unmapped", $"Choose an instrument and a sub-portfolio for: {string.Join(", ", tickers)}.");
}
