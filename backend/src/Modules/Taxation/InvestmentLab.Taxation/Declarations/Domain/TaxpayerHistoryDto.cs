using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record TaxpayerHistoryDto(
    IReadOnlyList<TaxpayerAccountHistory> Accounts, IReadOnlyList<LossYear> Losses, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete);
