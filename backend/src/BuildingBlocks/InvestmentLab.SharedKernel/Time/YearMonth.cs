using System.Globalization;

namespace InvestmentLab.SharedKernel;

public readonly record struct YearMonth : IComparable<YearMonth>
{
    public int Year { get; }
    public int Month { get; }

    private YearMonth(int year, int month) => (Year, Month) = (year, month);

    public static YearMonth Of(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);

        return new(year, month);
    }

    public static YearMonth Containing(DateOnly date) => new(date.Year, date.Month);

    public static Result<YearMonth> TryParse(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? Containing(date)
            : Error.Validation("year_month.invalid", $"'{text}' is not a yyyy-MM month.");

    public DateOnly FirstDay => new(Year, Month, 1);
    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public YearMonth Next() => Containing(FirstDay.AddMonths(1));
    public YearMonth Previous() => Containing(FirstDay.AddMonths(-1));

    public int CompareTo(YearMonth other) => (Year, Month).CompareTo((other.Year, other.Month));

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;
    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;
    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;
    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
