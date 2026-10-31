namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed record LoadOperationsPreviewDto(IReadOnlyList<ActionMatchDto> Matches, IReadOnlyList<MatchedOperationDto> Unmatched);
