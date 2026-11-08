using System.Globalization;
using InvestmentLab.SharedKernel;
using Microsoft.AspNetCore.Localization;

namespace InvestmentLab.Api.Hosting;

public static class RequestLanguage
{
    public static void AddRequestLanguage(this WebApplicationBuilder builder) =>
        builder.Services.AddRequestLocalization(options =>
        {
            var formatting = CultureInfo.CurrentCulture;
            var languages = Language.All.Select(language => CultureInfo.GetCultureInfo(language.Code)).ToList();

            options.DefaultRequestCulture = new RequestCulture(formatting, CultureInfo.GetCultureInfo(Language.Default.Code));
            options.SupportedCultures = [formatting];
            options.SupportedUICultures = languages;
            options.ApplyCurrentCultureToResponseHeaders = true;
        });
}
