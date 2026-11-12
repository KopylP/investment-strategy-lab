using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchCalendarYearDto(
    int Year,
    double Return,
    bool Partial)
{
    public static ResearchCalendarYearDto From(CalendarYearReturn calendarYearReturn) => new(
        Year: calendarYearReturn.Year,
        Return: calendarYearReturn.Return,
        Partial: calendarYearReturn.Partial);
}
