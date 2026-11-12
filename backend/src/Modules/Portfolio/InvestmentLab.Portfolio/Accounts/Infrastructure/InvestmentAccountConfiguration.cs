using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal sealed class InvestmentAccountConfiguration : IEntityTypeConfiguration<InvestmentAccount>
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

        account.HasMany(a => a.SubPortfolios).WithOne().HasForeignKey("AccountId").OnDelete(DeleteBehavior.Cascade);
        account.Navigation(a => a.SubPortfolios).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
