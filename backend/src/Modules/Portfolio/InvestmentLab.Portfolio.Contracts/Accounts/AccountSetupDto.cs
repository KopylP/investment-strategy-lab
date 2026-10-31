namespace InvestmentLab.Portfolio.Contracts.Accounts;

public sealed record AccountSetupDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Broker,
    string BaseCurrency,
    string TaxPolicyCode,
    string ContributionPolicy,
    decimal DriftBand,
    IReadOnlyList<SubPortfolioSetupDto> SubPortfolios);
