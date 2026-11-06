using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Localization;

public sealed partial class MessageLocalizer(IReadOnlyDictionary<string, string>? extraMessages = null)
{
    private const string CatalogResource = "locales.uk.json";
    private const string ListSeparator = " ";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Catalog = new(LoadCatalog);
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static MessageLocalizer Default { get; } = new();

    public static IReadOnlyDictionary<string, string> GlobalCatalog => Catalog.Value;

    public string Render(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? "";

        var token = TokenPattern().Match(text);
        if (!token.Success)
            return text;

        var code = token.Groups["code"].Value;
        var template = TemplateOf(code);
        if (template is null)
            return text;

        var parameters = ParametersOf(token.Groups["parameters"].Value);

        return PlaceholderPattern().Replace(template, placeholder => Fill(placeholder, parameters));
    }

    public string Render(string code, object? parameters) => Render(Message.Of(code, parameters));

    private string? TemplateOf(string code)
    {
        if (extraMessages is not null && extraMessages.TryGetValue(code, out var own))
            return own;

        return Catalog.Value.GetValueOrDefault(code);
    }

    private static Dictionary<string, JsonElement> ParametersOf(string json) =>
        json.Length == 0 ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];

    private string Fill(Match placeholder, Dictionary<string, JsonElement> parameters)
    {
        var name = placeholder.Groups["name"].Value;
        var format = placeholder.Groups["format"].Value;
        var argument = placeholder.Groups["argument"].Value;

        if (!parameters.TryGetValue(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return "";

        return format switch
        {
            "percent" => Percent(value.GetDecimal()),
            "signedPercent" => SignedPercent(value.GetDecimal()),
            "money" => Money(value.GetDecimal(), TextOf(parameters.GetValueOrDefault(argument))),
            "amount" => Amount(value.GetDecimal()),
            "plural" => Plural(value.GetDecimal(), argument),
            "join" => string.Join(argument, value.EnumerateArray().Select(item => Render(TextOf(item)))),
            _ when value.ValueKind == JsonValueKind.Array => string.Join(ListSeparator, value.EnumerateArray().Select(item => Render(TextOf(item)))),
            _ => Render(TextOf(value)),
        };
    }

    private static string TextOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Undefined or JsonValueKind.Null => "",
        JsonValueKind.Number => value.GetDecimal().ToString(Invariant),
        _ => value.ToString(),
    };

    private static string Percent(decimal fraction) =>
        decimal.Round(fraction * 100m, 0, MidpointRounding.ToEven).ToString("0", Invariant) + "%";

    private static string SignedPercent(decimal fraction)
    {
        var value = decimal.Round(fraction * 100m, 1, MidpointRounding.ToEven);
        var sign = value < 0m ? "-" : "+";

        return sign + Math.Abs(value).ToString("0.0", Invariant) + "%";
    }

    private static string Money(decimal amount, string currency)
    {
        var prefix = currency switch
        {
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            "PLN" => "zł ",
            _ => currency + " ",
        };
        var rounded = decimal.Round(Math.Abs(amount), 2, MidpointRounding.ToEven);
        var sign = amount < 0m ? "-" : "";

        return sign + prefix + rounded.ToString("#,##0.00", Invariant);
    }

    private static string Amount(decimal amount) => amount.ToString("0.00", Invariant);

    private static string Plural(decimal count, string forms)
    {
        var options = forms.Split('|');
        var whole = (long)Math.Abs(count);
        var lastTwo = whole % 100;
        var last = whole % 10;

        if (lastTwo is >= 11 and <= 14)
            return options[2];

        return last switch
        {
            1 => options[0],
            >= 2 and <= 4 => options[1],
            _ => options[2],
        };
    }

    private static IReadOnlyDictionary<string, string> LoadCatalog()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(CatalogResource, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resource)!;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    [GeneratedRegex(@"^@(?<code>[A-Za-z0-9_.\-]+)(?: (?<parameters>\{.*\}))?$", RegexOptions.Singleline)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"\{(?<name>[A-Za-z0-9_]+)(?::(?<format>[A-Za-z]+)(?::(?<argument>[^}]*))?)?\}")]
    private static partial Regex PlaceholderPattern();
}
