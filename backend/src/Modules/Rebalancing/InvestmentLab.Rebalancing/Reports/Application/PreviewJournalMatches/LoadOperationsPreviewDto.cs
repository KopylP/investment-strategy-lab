using InvestmentLab.Rebalancing.Reports.Application.Shared;

namespace InvestmentLab.Rebalancing.Reports.Application.PreviewJournalMatches;

public sealed record LoadOperationsPreviewDto(IReadOnlyList<ActionMatchDto> Matches, IReadOnlyList<MatchedOperationDto> Unmatched);
