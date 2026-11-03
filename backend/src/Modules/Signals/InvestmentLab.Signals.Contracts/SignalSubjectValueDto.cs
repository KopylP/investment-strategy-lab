namespace InvestmentLab.Signals.Contracts;

public sealed record SignalSubjectValueDto(string Key, decimal Value, DateOnly? HeldSince = null, decimal? UnitCost = null);
