namespace InvestmentLab.Backtesting.Replays.Domain;

// Daily-close replay supports the default end-of-day report. Other times need intraday quotes.
public sealed record ReplaySchedule(ReplayCadence Cadence, int Day = 1, DayOfWeek Weekday = DayOfWeek.Sunday);
