namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftDto(
    Guid Id,
    string Name,
    string? Description,
    int Revision,
    DateTimeOffset UpdatedAt,
    DraftSetupDto Setup,
    GraphDto Graph,
    LayoutDto Layout,
    IReadOnlyList<DraftReportDto> Reports,
    Guid? OriginStrategyId,
    int? OriginVersion,
    IReadOnlyList<DraftProblemDto> Problems,
    IReadOnlyList<BlockWordsDto> Words);
