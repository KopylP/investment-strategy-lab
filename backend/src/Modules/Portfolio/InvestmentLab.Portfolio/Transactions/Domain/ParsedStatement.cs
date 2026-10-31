namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record ParsedStatement(string Broker, IReadOnlyList<StatementRow> Rows, IReadOnlyList<string> Warnings);
