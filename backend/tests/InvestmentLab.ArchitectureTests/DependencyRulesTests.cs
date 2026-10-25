namespace InvestmentLab.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Theory]
    // inside a feature
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "InvestmentLab.Rebalancing.Reports.Domain")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "InvestmentLab.Rebalancing.Reports.Application.GetReport")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "InvestmentLab.Rebalancing.Reports.Domain")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Api", "InvestmentLab.Rebalancing.Reports.Application.GetReport")]
    // between features of one module: only through Domain
    [InlineData("InvestmentLab.Rebalancing.Planning.Domain", "InvestmentLab.Rebalancing.Reports.Domain")]
    [InlineData("InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport", "InvestmentLab.Rebalancing.Reports.Domain")]
    // module-wide shared infrastructure and the module root
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "InvestmentLab.Rebalancing.Shared.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Shared.Infrastructure", "InvestmentLab.Rebalancing.Reports.Domain")]
    [InlineData("InvestmentLab.Rebalancing", "InvestmentLab.Rebalancing.Reports.Api")]
    // other modules only through Contracts
    [InlineData("InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport", "InvestmentLab.Portfolio.Contracts")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "InvestmentLab.Portfolio.Contracts.Holdings")]
    // building blocks
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "InvestmentLab.SharedKernel")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Api", "InvestmentLab.BuildingBlocks.Http")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "Microsoft.EntityFrameworkCore")]
    public void Allows(string from, string to)
    {
        DependencyRules.Check(from, to).ShouldBeNull();
    }

    [Theory]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "InvestmentLab.Rebalancing.Reports.Application.GetReport")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "InvestmentLab.Rebalancing.Reports.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "InvestmentLab.Rebalancing.Shared.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "InvestmentLab.Rebalancing.Reports.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "InvestmentLab.Rebalancing.Reports.Api")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Api", "InvestmentLab.Rebalancing.Reports.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport", "InvestmentLab.Rebalancing.Reports.Application.GetReport")]
    [InlineData("InvestmentLab.Rebalancing.Planning.Api", "InvestmentLab.Rebalancing.Reports.Api")]
    [InlineData("InvestmentLab.Rebalancing.Planning.Infrastructure", "InvestmentLab.Rebalancing.Reports.Infrastructure")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "InvestmentLab.Portfolio.Accounts.Domain")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Infrastructure", "InvestmentLab.Portfolio")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "Microsoft.EntityFrameworkCore")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "Microsoft.AspNetCore.Http")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "Wolverine")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Domain", "InvestmentLab.BuildingBlocks.Http")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "Microsoft.AspNetCore.Http")]
    [InlineData("InvestmentLab.Rebalancing.Reports.Application.GetReport", "Microsoft.EntityFrameworkCore")]
    [InlineData("InvestmentLab.SharedKernel", "InvestmentLab.BuildingBlocks")]
    [InlineData("InvestmentLab.SharedKernel", "Microsoft.EntityFrameworkCore")]
    [InlineData("InvestmentLab.Portfolio.Contracts", "InvestmentLab.SharedKernel")]
    [InlineData("InvestmentLab.Portfolio.Contracts", "InvestmentLab.Portfolio.Accounts.Domain")]
    [InlineData("InvestmentLab.Portfolio.Contracts", "InvestmentLab.MarketData.Contracts")]
    public void Forbids(string from, string to)
    {
        DependencyRules.Check(from, to).ShouldNotBeNull();
    }
}
