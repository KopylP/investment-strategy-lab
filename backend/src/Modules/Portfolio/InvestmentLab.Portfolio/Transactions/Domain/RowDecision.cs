namespace InvestmentLab.Portfolio.Transactions.Domain;

public enum RowDecision
{
    New,

    AlreadyImported,

    Differs,

    MatchesExisting,

    OutOfScope,

    Unsupported,

    NeedsMapping,
}
