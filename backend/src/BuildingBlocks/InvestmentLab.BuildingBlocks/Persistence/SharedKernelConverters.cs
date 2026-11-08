using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.BuildingBlocks.Persistence;

public static class SharedKernelConverters
{
    public sealed class CurrencyConverter() : ValueConverter<Currency, string>(currency => currency.Code, code => Currency.Of(code));

    public sealed class LanguageConverter() : ValueConverter<Language, string>(language => language.Code, code => Language.Of(code));

    public sealed class IsinConverter() : ValueConverter<Isin, string>(isin => isin.Value, value => Isin.Of(value));

    public sealed class UserIdConverter() : ValueConverter<UserId, Guid>(id => id.Value, value => UserId.Of(value));

    public sealed class YearMonthConverter() : ValueConverter<YearMonth, string>(period => period.ToString(), text => YearMonth.TryParse(text).Value);

    public sealed class QuantityConverter() : ValueConverter<Quantity, decimal>(quantity => quantity.Value, value => Quantity.Of(value));

    public sealed class PercentageConverter() : ValueConverter<Percentage, decimal>(share => share.Value, value => Percentage.Of(value));
}
