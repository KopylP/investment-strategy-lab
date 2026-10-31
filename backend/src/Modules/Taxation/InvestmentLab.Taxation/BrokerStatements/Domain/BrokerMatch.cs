namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerMatch(IReadOnlyDictionary<Guid, SaleFacts> Sales, IReadOnlyDictionary<Guid, WithholdingFacts> Dividends);
