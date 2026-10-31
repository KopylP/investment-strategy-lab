using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;
using InvestmentLab.Performance.Valuation.Application.GetDashboard;
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
        Guid accountId, string? period, GetDashboardHandler handler, CancellationToken cancellationToken)
    {
        var dashboard = await handler.Handle(accountId, period, cancellationToken);

        return dashboard.ToHttpResult();
    }

    private static async Task<IResult> GetAllAccountsDashboard(
        string? currency, string? period, GetAllAccountsDashboardHandler handler, CancellationToken cancellationToken)
    {
        var dashboard = await handler.Handle(currency, period, cancellationToken);

        return dashboard.ToHttpResult();
    }
}
