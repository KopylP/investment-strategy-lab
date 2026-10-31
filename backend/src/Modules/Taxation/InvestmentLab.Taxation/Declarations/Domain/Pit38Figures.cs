namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record Pit38Figures(
    DeclarationInput Input,
    Pit38BrokerRows Rows,
    Pit38CapitalGains Gains,
    Pit38Dividends Dividends,
    decimal Rate,
    IReadOnlyList<DeclarationAttachment> Attachments,
    int SalesWithUnknownCountry);
