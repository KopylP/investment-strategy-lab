using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record RecomputeOutcome(
    IReadOnlyList<int> ChangedYears, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete, Taxpayer Taxpayer, TaxComputation Computation,
    IReadOnlyList<TaxEvent> Events, IReadOnlyList<LossYear> Losses);
