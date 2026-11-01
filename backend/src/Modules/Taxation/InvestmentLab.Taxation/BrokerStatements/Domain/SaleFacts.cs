namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record SaleFacts(IReadOnlyList<BrokerLotFact> Lots, decimal GainInTaxCurrency, string Currency, decimal? Cost);
