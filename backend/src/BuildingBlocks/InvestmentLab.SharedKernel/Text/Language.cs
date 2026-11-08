namespace InvestmentLab.SharedKernel;

public sealed record Language
{
    public static readonly Language Ukrainian = new("uk");
    public static readonly Language English = new("en");

    public static readonly Language Default = English;

    public static IReadOnlyList<Language> All { get; } = [Ukrainian, English];

    public string Code { get; }

    private Language(string code) => Code = code;

    public static Language Of(string code) =>
        TryOf(code) is { IsSuccess: true } result ? result.Value : throw new ArgumentException($"'{code}' is not a language the service speaks.", nameof(code));

    public static Result<Language> TryOf(string? code)
    {
        var known = All.FirstOrDefault(language => language.Code == code?.Trim().ToLowerInvariant());
        if (known is null)
            return Error.Validation("language.unsupported", $"'{code}' is not a language the service speaks.", new { code });

        return known;
    }

    public static Language OfCultureOrDefault(string cultureName)
    {
        var languageCode = cultureName.Split('-')[0];
        var known = TryOf(languageCode);

        return known.IsSuccess ? known.Value : Default;
    }

    public override string ToString() => Code;
}
