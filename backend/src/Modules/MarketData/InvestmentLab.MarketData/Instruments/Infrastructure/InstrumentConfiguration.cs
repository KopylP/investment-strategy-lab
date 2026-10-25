using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> instrument)
    {
        instrument.ToTable("instruments");
        instrument.HasKey(i => i.Id);
        instrument.Property(i => i.Id).HasConversion<MarketDataConverters.InstrumentIdConverter>();
        instrument.Property(i => i.Kind).HasConversion<string>().HasMaxLength(20);
        instrument.Property(i => i.Name).HasMaxLength(200);
        instrument.Property(i => i.Exchange).HasMaxLength(10);
        instrument.Property(i => i.Group).HasConversion(group => group!.Name, name => InstrumentGroup.Of(name)).HasMaxLength(100);
        instrument.HasIndex(i => i.Isin).IsUnique().HasFilter("isin IS NOT NULL");

        instrument.OwnsMany(i => i.ProviderSymbols, symbols =>
        {
            symbols.ToTable("instrument_provider_symbols");
            symbols.WithOwner().HasForeignKey("InstrumentId");
            symbols.Property(s => s.Provider).HasConversion(provider => provider.Code, code => QuoteProvider.Of(code)).HasMaxLength(30);
            symbols.Property(s => s.Symbol).HasMaxLength(50);
            // One symbol per provider and instrument; a symbol names exactly one instrument.
            symbols.HasKey("InstrumentId", nameof(ProviderSymbol.Provider));
            symbols.HasIndex(s => new { s.Provider, s.Symbol }).IsUnique();
        });
        instrument.Navigation(i => i.ProviderSymbols).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
