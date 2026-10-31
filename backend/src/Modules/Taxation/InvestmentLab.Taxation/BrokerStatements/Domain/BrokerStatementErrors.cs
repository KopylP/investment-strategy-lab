namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public static class BrokerStatementErrors
{
    public static readonly Error Empty =
        Error.Validation(
            "broker_statement.empty",
            "No sold lots or dividends were found: the file does not look like a Revolut Profit and Loss Statement (PDF).");

    public static Error Unreadable(string details) => Error.Validation("broker_statement.unreadable", $"The PDF could not be read: {details}");

    public static Error NotFound(Guid statementId) => Error.NotFound("broker_statement.not_found", $"Broker statement {statementId} does not exist.");
}
