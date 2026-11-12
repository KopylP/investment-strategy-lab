using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Ledger.Domain;
using Microsoft.EntityFrameworkCore;

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
        ConfigureAccounts(modelBuilder);
        ConfigureLedgerEntries(modelBuilder);
        ConfigureAssessments(modelBuilder);
        ConfigureLossCarryforwards(modelBuilder);
        ConfigureBrokerStatements(modelBuilder);
        ConfigurePit8CStatements(modelBuilder);
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<TaxAccount>();

        account.ToTable("accounts");
        account.HasKey(a => a.AccountId);
        account.Property(a => a.Currency).HasMaxLength(3);
        account.Property(a => a.PolicyCode).HasMaxLength(20);

        HasOwnerFilter(account, a => a.OwnerId);
    }

    private static void ConfigureLedgerEntries(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<TaxLedgerEntry>();

        entry.ToTable("ledger_entries");
        entry.HasKey(e => e.TransactionId);
        entry.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
        entry.Property(e => e.Currency).HasMaxLength(3);
        entry.Property(e => e.Allocations).HasJsonConversion();
        entry.HasIndex(e => e.AccountId);
        entry.Ignore(e => e.TaxYear);

        HasOwnerFilter(entry, e => e.OwnerId);
    }

    private static void ConfigureAssessments(ModelBuilder modelBuilder)
    {
        var assessment = modelBuilder.Entity<TaxAssessment>();

        assessment.Ignore(a => a.DomainEvents);
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
    }

    private static void ConfigureLossCarryforwards(ModelBuilder modelBuilder)
    {
        var loss = modelBuilder.Entity<LossCarryforward>();

        loss.ToTable("loss_carryforwards");
        loss.HasKey(l => l.Id);
        loss.HasIndex(l => l.OwnerId);

        HasOwnerFilter(loss, l => l.OwnerId);
    }

    private static void ConfigureBrokerStatements(ModelBuilder modelBuilder)
    {
        var statement = modelBuilder.Entity<BrokerStatement>();

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
    }

    private static void ConfigurePit8CStatements(ModelBuilder modelBuilder)
    {
        var statement = modelBuilder.Entity<Pit8CStatement>();

        statement.ToTable("pit8c_statements");
        statement.HasKey(s => s.Id);
        statement.HasIndex(s => new { s.AccountId, s.Year }).IsUnique();
        statement.Ignore(s => s.Amounts);

        HasOwnerFilter(statement, s => s.OwnerId);
    }
}
