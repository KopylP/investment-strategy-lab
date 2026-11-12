using System.Text.Json;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class StatementImportConfiguration : IEntityTypeConfiguration<StatementImport>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<StatementImport> statement)
    {
        statement.ToTable("statement_imports");
        statement.HasKey(s => s.Id);
        statement.Property(s => s.Id).ValueGeneratedNever();
        statement.Property(s => s.AccountId).HasConversion<PortfolioConverters.AccountIdConverter>();
        statement.Property(s => s.Broker).HasMaxLength(50);
        statement.Property(s => s.FileName).HasMaxLength(300);
        statement.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        statement.Property(s => s.Mode).HasConversion<string>().HasMaxLength(20);
        Jsonb(statement.Property(s => s.Rows));
        Jsonb(statement.Property(s => s.Mappings));
        Jsonb(statement.Property(s => s.Warnings));
        Jsonb(statement.Property(s => s.Outcomes));
        Jsonb(statement.Property(s => s.RecordedReferences));
        Jsonb(statement.Property(s => s.ReversedTransactionIds));
        statement.Ignore(s => s.IsPreview);
        statement.HasIndex(s => s.AccountId);
    }

    private static void Jsonb<T>(PropertyBuilder<T> property) =>
        property.HasConversion(
                new ValueConverter<T, string>(value => JsonSerializer.Serialize(value, Json), text => JsonSerializer.Deserialize<T>(text, Json)!),
                new ValueComparer<T>(
                    (left, right) => JsonSerializer.Serialize(left, Json) == JsonSerializer.Serialize(right, Json),
                    value => JsonSerializer.Serialize(value, Json).GetHashCode(),
                    value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!))
            .HasColumnType("jsonb");
}
