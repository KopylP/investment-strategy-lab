using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Accounts.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Portfolio.Transactions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "portfolio";

    public DbSet<InvestmentAccount> Accounts => Set<InvestmentAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<HoldingRow> HoldingRows => Set<HoldingRow>();
    public DbSet<StatementImport> StatementImports => Set<StatementImport>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override Assembly MappingsAssembly => typeof(PortfolioAssemblyMarker).Assembly;
}
