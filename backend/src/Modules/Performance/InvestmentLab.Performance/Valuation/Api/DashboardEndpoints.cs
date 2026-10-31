using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Performance.Valuation.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Performance.Valuation.Api;

internal static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboard(this RouteGroupBuilder group)
    {
        group.MapGet("/accounts/{accountId:guid}/dashboard", GetDashboard)
            .WithTags("Performance")
            .WithName("GetDashboard");

        group.MapGet("/dashboard", GetAllAccountsDashboard)
            .WithTags("Performance")
            .WithName("GetAllAccountsDashboard");

        return group;
    }

    private static async Task<IResult> GetDashboard(
        Guid accountId, string? period, DashboardQueries queries, CancellationToken cancellationToken)
    {
        var dashboard = await queries.Get(accountId, period, cancellationToken);

        return dashboard.ToHttpResult();
    }

    private static async Task<IResult> GetAllAccountsDashboard(
        string? currency, string? period, DashboardQueries queries, CancellationToken cancellationToken)
    {
        var dashboard = await queries.GetAll(currency, period, cancellationToken);

        return dashboard.ToHttpResult();
    }
}
