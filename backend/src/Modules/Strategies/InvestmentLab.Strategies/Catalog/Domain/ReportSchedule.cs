namespace InvestmentLab.Strategies.Catalog.Domain;

public sealed record ReportSchedule(string Frequency, int Day, string Weekday, string Time, int Minute)
{
    public static readonly ReportSchedule Off = new("off", 1, "sunday", "20:00", 0);
}
