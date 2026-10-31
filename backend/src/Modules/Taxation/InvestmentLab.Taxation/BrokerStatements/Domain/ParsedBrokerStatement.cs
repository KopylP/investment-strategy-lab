namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record ParsedBrokerStatement(string Broker, IReadOnlyList<BrokerSaleLot> Lots, IReadOnlyList<BrokerDividend> Dividends, IReadOnlyList<string> Warnings);
