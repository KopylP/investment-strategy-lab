namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record EvaluateSubPortfolioCommand(
    Guid AccountId,
    Guid SubPortfolioId,
    DateOnly? AsOf = null,
    decimal Contribution = 0m,
    int? Version = null);
