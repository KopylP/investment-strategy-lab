namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record WithholdingFacts(decimal GrossPerNet, decimal WithheldPerNet);
