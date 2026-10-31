using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed class Pit38ForeignSales(IReadOnlyList<DeclarationAccount> withoutPit8CForm, Func<Guid, string?> countryOf)
{
    private const string AttachmentForm = "PIT/ZG";
    private const string PolandCode = "PL";

    private readonly List<Realization> _sales = [.. withoutPit8CForm.SelectMany(account => account.Sales)];

    public int WithUnknownCountry =>
        _sales.Count(sale => !IsCountry(countryOf(sale.InstrumentId)) && countryOf(sale.InstrumentId) != PolandCode);

    public List<DeclarationAttachment> AttachmentsWhen(decimal income)
    {
        if (income <= 0m)
            return [];

        var byCountry = _sales
            .GroupBy(sale => countryOf(sale.InstrumentId))
            .Where(group => IsCountry(group.Key))
            .Select(group => (Code: group.Key!, Income: group.Sum(sale => sale.GainPln)))
            .Where(country => country.Income > 0m)
            .OrderBy(country => country.Code);

        return [.. byCountry.Select(country => AttachmentOf(country.Code, country.Income))];
    }

    private static DeclarationAttachment AttachmentOf(string code, decimal income)
    {
        var name = Countries.Name(code);

        return new DeclarationAttachment(AttachmentForm, $"PIT/ZG · {name}", [
            new(6, "Państwo uzyskania dochodu", null, name),
            new(7, "Kod kraju", null, code),
            new(29, "Dochód, o którym mowa w art. 30b ust. 5a i 5b", income, Hint: "Прибуток від продажу паперів цієї країни (за ISIN)"),
            new(30, "Podatek zapłacony za granicą od dochodów z poz. 29", 0m, Hint: "Брокер податку з продажу не утримує"),
        ]);
    }

    private static bool IsCountry(string? code) => code is { Length: 2 } && code != PolandCode && code != "EU" && !code.StartsWith('X');
}
