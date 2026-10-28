using System.Text.Json;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Ledger.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Taxation.Shared.Infrastructure;

public sealed class TaxationDbContext(DbContextOptions<TaxationDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "taxation";

    public DbSet<TaxAccount> Accounts => Set<TaxAccount>();
    public DbSet<TaxLedgerEntry> Entries => Set<TaxLedgerEntry>();
    public DbSet<TaxAssessment> Assessments => Set<TaxAssessment>();
    public DbSet<LossCarryforward> Losses => Set<LossCarryforward>();
    public DbSet<BrokerStatement> BrokerStatements => Set<BrokerStatement>();
    public DbSet<Pit8CStatement> Pit8CStatements => Set<Pit8CStatement>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaxAccount>(account =>
        {
            account.ToTable("accounts");
            account.HasKey(a => a.AccountId);
            account.Property(a => a.Currency).HasMaxLength(3);
            account.Property(a => a.PolicyCode).HasMaxLength(20);
            HasOwnerFilter(account, a => a.OwnerId);
        });

        modelBuilder.Entity<TaxLedgerEntry>(entry =>
        {
            entry.ToTable("ledger_entries");
            entry.HasKey(e => e.TransactionId);
            entry.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
            entry.Property(e => e.Currency).HasMaxLength(3);
            entry.Property(e => e.Allocations).HasJsonConversion();
            entry.HasIndex(e => e.AccountId);
            entry.Ignore(e => e.TaxYear);
            HasOwnerFilter(entry, e => e.OwnerId);
        });

        modelBuilder.Entity<TaxAssessment>(assessment =>
        {
            assessment.ToTable("assessments");
            assessment.HasKey(a => a.Id);
            assessment.HasIndex(a => new { a.OwnerId, a.Year }).IsUnique();
            assessment.Property(a => a.PolicyCode).HasMaxLength(20);
            assessment.Property(a => a.InputHash).HasMaxLength(64);
            assessment.Property(a => a.Summary).HasJsonConversion();
            assessment.Property(a => a.Realizations).HasJsonConversion();
            assessment.Property(a => a.Dividends).HasJsonConversion();
            assessment.Property(a => a.CarryforwardUses).HasJsonConversion();
            HasOwnerFilter(assessment, a => a.OwnerId);
        });

        modelBuilder.Entity<LossCarryforward>(loss =>
        {
            loss.ToTable("loss_carryforwards");
            loss.HasKey(l => l.Id);
            loss.HasIndex(l => l.OwnerId);
            HasOwnerFilter(loss, l => l.OwnerId);
        });

        modelBuilder.Entity<BrokerStatement>(statement =>
        {
            statement.ToTable("broker_statements");
            statement.HasKey(s => s.Id);
            statement.HasIndex(s => s.AccountId);
            statement.Property(s => s.Broker).HasMaxLength(40);
            statement.Property(s => s.FileName).HasMaxLength(260);
            statement.Property(s => s.Lots).HasJsonConversion();
            statement.Property(s => s.Dividends).HasJsonConversion();
            statement.Property(s => s.Warnings).HasJsonConversion();
            statement.Ignore(s => s.Span);
            HasOwnerFilter(statement, s => s.OwnerId);
        });

        modelBuilder.Entity<Pit8CStatement>(statement =>
        {
            statement.ToTable("pit8c_statements");
            statement.HasKey(s => s.Id);
            statement.HasIndex(s => new { s.AccountId, s.Year }).IsUnique();
            statement.Ignore(s => s.Amounts);
            HasOwnerFilter(statement, s => s.OwnerId);
        });
    }
}

/// <summary>Results and allocations without rules of their own are stored as jsonb.</summary>
internal static class JsonColumn
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> property)
    {
        var converter = new ValueConverter<T, string>(value => JsonSerializer.Serialize(value, Options), json => JsonSerializer.Deserialize<T>(json, Options)!);
        var comparer = new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
            value => JsonSerializer.Serialize(value, Options).GetHashCode(),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!);
        return property.HasConversion(converter, comparer).HasColumnType("jsonb");
    }
}

internal sealed class TaxationDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<TaxationDbContext>
{
    protected override string Schema => TaxationDbContext.SchemaName;

    protected override TaxationDbContext Create(DbContextOptions<TaxationDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
