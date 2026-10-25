using InvestmentLab.MarketData.FxRates.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.MarketData.FxRates.Infrastructure;

internal sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> rate)
    {
        rate.ToTable("fx_rates");
        rate.HasKey(r => new { r.Source, r.Base, r.Quote, r.EffectiveDate });
        rate.Property(r => r.Source).HasConversion<string>().HasMaxLength(10);
        rate.Property(r => r.TableNumber).HasMaxLength(30);
    }
}
