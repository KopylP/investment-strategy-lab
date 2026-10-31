using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class QuoteCorrectionConfiguration : IEntityTypeConfiguration<QuoteCorrection>
{
    public void Configure(EntityTypeBuilder<QuoteCorrection> correction)
    {
        correction.ToTable("quote_corrections");
        correction.HasKey(c => c.Id);
        correction.Property(c => c.Id).HasConversion(id => id.Value, value => QuoteCorrectionId.Of(value));
        correction.Property(c => c.InstrumentId).HasConversion<MarketDataConverters.InstrumentIdConverter>();
        correction.Property(c => c.Reason).HasMaxLength(500);
        correction.Property(c => c.CorrectedBy).HasMaxLength(200);
        correction.HasIndex(c => new { c.InstrumentId, c.TradingDate });
    }
}
