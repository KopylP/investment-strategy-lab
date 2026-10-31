namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record BrokerStatementsDto(IReadOnlyList<BrokerStatementDto> Statements, int Sales, int MatchedSales, int Dividends, int MatchedDividends);
