namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyTypeSchemaDto(string Code, string ParametersSchema, IReadOnlyList<StrategyReportDto> Reports);
