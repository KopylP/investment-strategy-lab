using System.Globalization;

namespace InvestmentLab.Taxation.Declarations.Domain;

public static class Pit38Amounts
{
    public static decimal Grosze(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static string Typed(decimal amount) =>
        decimal.IsInteger(amount)
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
}
