namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyReportScheduleDto(string Frequency, int Day, string Weekday, string Time, int Minute);
