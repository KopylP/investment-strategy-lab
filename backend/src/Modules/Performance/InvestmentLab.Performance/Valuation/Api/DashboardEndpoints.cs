using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;
using InvestmentLab.Performance.Valuation.Application.GetDashboard;
using InvestmentLab.Performance.Valuation.Application.RefreshSnapshots;
using InvestmentLab.Performance.Valuation.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Performance.Valuation.Api;

internal static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboard(this RouteGroupBuilder group)
    {
        group
            .MapGet("/accounts/{accountId:guid}/dashboard", RefreshAndGetDashboard)
            .WithTags("Performance")
            .WithName("GetDashboard")
            .Produces<DashboardDto>();

        group
            .MapGet("/dashboard", RefreshAndGetAllAccountsDashboard)
            .WithTags("Performance")
            .WithName("GetAllAccountsDashboard")
            .Produces<DashboardDto>();

        return group;
    }

    private static async Task<IResult> RefreshAndGetDashboard(
        Guid accountId,
        string? period,
        RefreshSnapshotsHandler refresh,
        GetDashboardHandler query,
        CancellationToken cancellationToken)
    {
        var refreshed = await refresh.Handle(new RefreshSnapshotsCommand(accountId), cancellationToken);
        if (refreshed.IsFailure)
            return refreshed.ToHttpResult();

        var dashboard = await query.Handle(accountId, period, cancellationToken);

        return dashboard.ToHttpResult();
    }

    private static async Task<IResult> RefreshAndGetAllAccountsDashboard(
        string? currency,
        string? period,
        RefreshSnapshotsHandler refresh,
        GetAllAccountsDashboardHandler query,
        CancellationToken cancellationToken)
    {
        var refreshed = await refresh.Handle(new RefreshSnapshotsCommand(AccountId: null), cancellationToken);
        if (refreshed.IsFailure)
            return refreshed.ToHttpResult();

        var dashboard = await query.Handle(currency, period, cancellationToken);

        return dashboard.ToHttpResult();
    }
}
