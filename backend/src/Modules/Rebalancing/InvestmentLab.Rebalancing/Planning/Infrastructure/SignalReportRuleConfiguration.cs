using InvestmentLab.Rebalancing.Planning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class SignalReportRuleConfiguration : IEntityTypeConfiguration<SignalReportRule>
{
    public void Configure(EntityTypeBuilder<SignalReportRule> rule)
    {
        rule.ToTable("signal_report_rules");
        rule.HasKey(r => r.Id);
        rule.Property(r => r.Id).ValueGeneratedNever();

        rule.Property(r => r.ReportCode).HasMaxLength(50);
        rule.Property(r => r.Problem).HasMaxLength(500);

        rule.Ignore(r => r.DomainEvents);

        rule.HasIndex(r => new { r.SignalId, r.AccountId, r.ReportCode }).IsUnique();
    }
}
