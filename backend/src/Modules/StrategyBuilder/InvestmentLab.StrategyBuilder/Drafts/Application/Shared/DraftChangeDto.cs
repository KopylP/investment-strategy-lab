namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftChangeDto(
    int Revision,
    string? Name,
    string? Description,
    DraftSetupDto? Setup,
    GraphDto? Graph,
    IReadOnlyList<DraftReportDto>? Reports);
