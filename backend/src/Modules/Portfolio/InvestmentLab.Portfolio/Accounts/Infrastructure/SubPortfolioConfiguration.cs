using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

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
        });
        sub.Ignore(s => s.IsActive);
        sub.HasIndex("AccountId", nameof(SubPortfolio.Position)).IsUnique();
    }
}
