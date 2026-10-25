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
        group.MapGet("/accounts/{accountId:guid}/dashboard", async (Guid accountId, string? period, DashboardQueries queries, CancellationToken ct) =>
                (await queries.Get(accountId, period, ct)).ToHttpResult())
            .WithTags("Performance")
            .WithName("GetDashboard");
        return group;
    }
}
