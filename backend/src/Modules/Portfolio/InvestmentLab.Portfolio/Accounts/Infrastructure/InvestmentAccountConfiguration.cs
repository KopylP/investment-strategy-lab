using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal sealed class InvestmentAccountConfiguration(PortfolioDbContext context) : IEntityTypeConfiguration<InvestmentAccount>
{
    public void Configure(EntityTypeBuilder<InvestmentAccount> account)
    {
        account.ToTable("accounts");
        account.HasKey(a => a.Id);
        account.Property(a => a.Id).HasConversion<PortfolioConverters.AccountIdConverter>().ValueGeneratedNever();
        account.Property(a => a.Name).HasMaxLength(100);
        account.Property(a => a.Broker).HasMaxLength(100);
        account.Property(a => a.TaxPolicyCode).HasMaxLength(30);
        account.HasIndex(a => a.OwnerId);
        account.ComplexProperty(a => a.ContributionPolicy, policy =>
        {
            policy.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20).HasColumnName("contribution_policy");
            policy.Property(p => p.DriftBand).HasColumnName("drift_band");
        });
        account.Ignore(a => a.ActiveSubPortfolios);
        context.HasOwnerFilter(account, a => a.OwnerId);

        account.HasMany(a => a.SubPortfolios).WithOne().HasForeignKey("AccountId").OnDelete(DeleteBehavior.Cascade);
        account.Navigation(a => a.SubPortfolios).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

internal sealed class SubPortfolioConfiguration : IEntityTypeConfiguration<SubPortfolio>
{
    public void Configure(EntityTypeBuilder<SubPortfolio> sub)
    {
        sub.ToTable("sub_portfolios");
        sub.HasKey(s => s.Id);
        sub.Property(s => s.Id).HasConversion<PortfolioConverters.SubPortfolioIdConverter>().ValueGeneratedNever();
        sub.Property(s => s.Name).HasMaxLength(100);
        sub.Property(s => s.TargetShare).HasConversion<PortfolioConverters.TargetShareConverter>();
        sub.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        sub.OwnsOne(s => s.Strategy, strategy =>
        {
            strategy.Property(r => r.StrategyId).HasColumnName("strategy_id");
            strategy.Property(r => r.PinnedVersion).HasColumnName("strategy_pinned_version");
        });
        sub.ComplexProperty(s => s.Guardrails, guardrails =>
        {
            guardrails.Property(g => g.MaxPositionWeight).HasColumnName("max_position_weight");
            guardrails.Property(g => g.MaxMonthlyTurnover).HasColumnName("max_monthly_turnover");
            guardrails.Ignore(g => g.AllowLeverage);
        });
        sub.Ignore(s => s.IsActive);
    }
}
