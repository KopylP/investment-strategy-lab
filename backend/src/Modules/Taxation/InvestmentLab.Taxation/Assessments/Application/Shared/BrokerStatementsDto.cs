namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record BrokerStatementsDto(IReadOnlyList<BrokerStatementDto> Statements, int Sales, int MatchedSales, int Dividends, int MatchedDividends);
