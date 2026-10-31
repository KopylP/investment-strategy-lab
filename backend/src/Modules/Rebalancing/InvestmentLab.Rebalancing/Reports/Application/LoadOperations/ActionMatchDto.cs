namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed record ActionMatchDto(Guid ActionId, int Sequence, string ActionKind, Guid? InstrumentId, decimal PlannedQuantity, decimal RemainingQuantity, bool Completes, IReadOnlyList<MatchedOperationDto> Operations);
