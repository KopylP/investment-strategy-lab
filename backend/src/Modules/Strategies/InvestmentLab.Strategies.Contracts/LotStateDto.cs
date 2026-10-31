namespace InvestmentLab.Strategies.Contracts;

public sealed record LotStateDto(decimal Quantity, decimal UnitCost, DateOnly? AcquiredOn);
