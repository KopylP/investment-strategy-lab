using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class DailyQuoteConfiguration : IEntityTypeConfiguration<DailyQuote>
{
    public void Configure(EntityTypeBuilder<DailyQuote> quote)
    {
        quote.ToTable("daily_quotes");
        quote.HasKey(q => new { q.InstrumentId, q.TradingDate });
        quote.Property(q => q.InstrumentId).HasConversion<MarketDataConverters.InstrumentIdConverter>();
        quote.Property(q => q.Source).HasMaxLength(30);
        quote.Ignore(q => q.IsCorrected);
        quote.Ignore(q => q.Range);
    }
}
