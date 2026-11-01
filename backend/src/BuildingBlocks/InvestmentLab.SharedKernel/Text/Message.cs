using System.Text.Encodings.Web;
using System.Text.Json;

namespace InvestmentLab.SharedKernel;

public static class Message
{
    public const char Prefix = '@';
    private const char ParametersSeparator = ' ';

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Of(string code) => $"{Prefix}{code}";

    public static string Of(string code, object? parameters)
    {
        if (parameters is null)
            return Of(code);

        return $"{Prefix}{code}{ParametersSeparator}{JsonSerializer.Serialize(parameters, Options)}";
    }
}
