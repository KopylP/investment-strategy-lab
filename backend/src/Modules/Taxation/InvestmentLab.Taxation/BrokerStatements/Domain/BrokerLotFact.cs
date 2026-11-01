namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerLotFact(DateOnly AcquiredOn, decimal Quantity, decimal Cost, decimal CostInTaxCurrency);
