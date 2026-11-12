using System.Reflection;
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

    protected override Assembly MappingsAssembly => typeof(TaxationAssemblyMarker).Assembly;
}
