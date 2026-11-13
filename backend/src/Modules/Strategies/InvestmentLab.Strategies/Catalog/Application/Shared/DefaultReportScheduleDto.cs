namespace InvestmentLab.Strategies.Catalog.Application.Shared;

public sealed record DefaultReportScheduleDto(string Frequency, int Day, string Weekday, string Time, int Minute);
