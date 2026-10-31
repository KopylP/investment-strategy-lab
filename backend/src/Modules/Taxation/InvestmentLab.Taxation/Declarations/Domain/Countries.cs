namespace InvestmentLab.Taxation.Declarations.Domain;

public static class Countries
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["US"] = "Stany Zjednoczone Ameryki",
        ["IE"] = "Irlandia",
        ["LU"] = "Luksemburg",
        ["DE"] = "Niemcy",
        ["NL"] = "Holandia",
        ["GB"] = "Wielka Brytania",
        ["FR"] = "Francja",
        ["CH"] = "Szwajcaria",
        ["JP"] = "Japonia",
        ["CA"] = "Kanada",
        ["ES"] = "Hiszpania",
        ["IT"] = "Włochy",
        ["BE"] = "Belgia",
        ["AT"] = "Austria",
        ["DK"] = "Dania",
        ["SE"] = "Szwecja",
        ["FI"] = "Finlandia",
        ["NO"] = "Norwegia",
        ["PT"] = "Portugalia",
        ["KY"] = "Kajmany",
        ["BM"] = "Bermudy",
        ["JE"] = "Jersey",
        ["CN"] = "Chiny",
        ["HK"] = "Hongkong",
        ["TW"] = "Tajwan",
        ["KR"] = "Korea Południowa",
        ["AU"] = "Australia",
        ["IL"] = "Izrael",
        ["LT"] = "Litwa",
        ["CZ"] = "Czechy",
    };

    public static string Name(string code) => Names.GetValueOrDefault(code, code);
}
