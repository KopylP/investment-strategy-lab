using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record CalendarYearReturnDto(
    int Year,
    double Return,
    bool Partial)
{
    public static CalendarYearReturnDto From(CalendarYearReturn calendarYearReturn) => new(
        Year: calendarYearReturn.Year,
        Return: calendarYearReturn.Return,
        Partial: calendarYearReturn.Partial);
}
