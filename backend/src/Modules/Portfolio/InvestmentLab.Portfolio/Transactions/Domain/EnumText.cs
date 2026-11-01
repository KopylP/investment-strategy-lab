namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class EnumText
{
    public static Result<TEnum> TryParse<TEnum>(string? text, string errorCode) where TEnum : struct, Enum
    {
        var isName = Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) && Enum.IsDefined(value) && !int.TryParse(text, out _);
        if (isName)
            return value;

        return Error.Validation(errorCode, $"'{text}' is not one of {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }
}
