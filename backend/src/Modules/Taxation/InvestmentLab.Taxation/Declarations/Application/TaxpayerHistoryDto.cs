using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record TaxpayerHistoryDto(
    IReadOnlyList<TaxpayerAccountHistory> Accounts, IReadOnlyList<LossYear> Losses, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete);
