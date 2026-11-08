using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Localization;

internal static class PluralForms
{
    private const char Separator = '|';

    private static readonly Dictionary<Language, Func<long, int>> FormOfCount = new()
    {
        [Language.Ukrainian] = UkrainianForm,
        [Language.English] = EnglishForm,
    };

    public static string Choose(Language language, decimal count, string forms)
    {
        var options = forms.Split(Separator);
        var whole = (long)Math.Abs(count);
        var index = FormOfCount[language](whole);

        return options[Math.Min(index, options.Length - 1)];
    }

    private static int EnglishForm(long whole) => whole == 1 ? 0 : 1;

    private static int UkrainianForm(long whole)
    {
        var lastTwo = whole % 100;
        var last = whole % 10;

        if (lastTwo is >= 11 and <= 14)
            return 2;

        return last switch
        {
            1 => 0,
            >= 2 and <= 4 => 1,
            _ => 2,
        };
    }
}
