using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal static class SubPortfolioMapping
{
    public const string JsonColumn = "sub_portfolios";

    public static void Configure(OwnedNavigationBuilder<InvestmentAccount, SubPortfolio> sub)
    {
        sub.ToJson(JsonColumn);
        sub.Property(s => s.Id).HasConversion<PortfolioConverters.SubPortfolioIdConverter>().ValueGeneratedNever();
        sub.Property(s => s.Name).HasMaxLength(100);
        sub.Property(s => s.TargetShare).HasConversion<PortfolioConverters.TargetShareConverter>();
        sub.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        sub.OwnsOne(s => s.Strategy);
        sub.OwnsOne(s => s.Guardrails);
        sub.Navigation(s => s.Guardrails).IsRequired();
        sub.Ignore(s => s.IsActive);
    }
}
