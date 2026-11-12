using InvestmentLab.Performance.Valuation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class RecalculationMarkerConfiguration : IEntityTypeConfiguration<RecalculationMarker>
{
    public void Configure(EntityTypeBuilder<RecalculationMarker> builder)
    {
        builder.ToTable("recalculation_markers");
        builder.HasKey(m => m.AccountId);
        builder.Ignore(m => m.IsDirty);
    }
}
