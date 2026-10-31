namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record SaleFacts(IReadOnlyList<BrokerLotFact> Lots, decimal GainPln, string Currency, decimal? Cost);
