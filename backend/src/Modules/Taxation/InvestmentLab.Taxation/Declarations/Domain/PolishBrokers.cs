using System.Text.RegularExpressions;

namespace InvestmentLab.Taxation.Declarations.Domain;

public static class PolishBrokers
{
    private static readonly string[] Names =
        [
            "XTB",
            "mBank",
            "mDM",
            "Bossa",
            "BOŚ",
            "PKO",
            "Pekao",
            "Santander",
            "ING",
            "Alior",
            "Millennium",
            "Noble",
            "BNP",
            "Trigon",
            "Ipopema",
            "Citi Handlowy",
            "BDM",
            "Michael/Ström",
            "Erste"];

    public static bool IssuesPit8C(string broker) => Key(broker) is not null;

    public static string? Key(string broker) =>
        Names.FirstOrDefault(name => Regex.IsMatch(broker, $@"\b{Regex.Escape(name)}\w{{0,2}}\b", RegexOptions.IgnoreCase));
}
