using System.Text.Json;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Application.ImportStatement;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class StatementImportConfiguration(PortfolioDbContext context) : IEntityTypeConfiguration<StatementImport>
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
        context.HasOwnerFilter(statement, s => s.OwnerId);
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

internal sealed class StatementImportRepository(PortfolioDbContext db) : IStatementImportRepository
{
    public Task<StatementImport?> FindAsync(Guid importId, CancellationToken cancellationToken) =>
        db.StatementImports.SingleOrDefaultAsync(statement => statement.Id == importId, cancellationToken);

    public async Task<IReadOnlyList<StatementImport>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await db.StatementImports.Where(statement => statement.AccountId == accountId).ToListAsync(cancellationToken);

    public void Add(StatementImport statementImport) => db.StatementImports.Add(statementImport);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
