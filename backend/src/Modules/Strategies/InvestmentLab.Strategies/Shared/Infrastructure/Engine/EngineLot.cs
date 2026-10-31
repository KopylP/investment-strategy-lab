namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineLot(decimal Quantity, decimal Price, DateOnly? AcquiredOn);
