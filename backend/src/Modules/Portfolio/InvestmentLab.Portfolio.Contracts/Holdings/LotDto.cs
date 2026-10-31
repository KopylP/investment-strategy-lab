namespace InvestmentLab.Portfolio.Contracts.Holdings;

public sealed record LotDto(Guid OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
