using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Assessments.Api;

internal static class BrokerStatementEndpoints
{
    public static RouteGroupBuilder MapBrokerStatements(this RouteGroupBuilder group)
    {
        var statements = group.MapGroup("/accounts/{accountId:guid}/broker-statements").WithTags("Taxes");

        statements.MapGet("", ListBrokerStatements).WithName("ListBrokerStatements");
        statements.MapPost("", UploadBrokerStatement).WithName("UploadBrokerStatement");
        statements.MapDelete("/{statementId:guid}", RemoveBrokerStatement).WithName("RemoveBrokerStatement");

        return group;
    }

    private static async Task<IResult> ListBrokerStatements(
        Guid accountId, BrokerStatementService service, CancellationToken cancellationToken)
    {
        var statements = await service.List(accountId, cancellationToken);

        return statements.ToHttpResult();
    }

    private static async Task<IResult> UploadBrokerStatement(
        Guid accountId, UploadBrokerStatementCommand body, BrokerStatementService service, CancellationToken cancellationToken)
    {
        var uploaded = await service.Upload(accountId, body, cancellationToken);

        return uploaded.ToHttpResult();
    }

    private static async Task<IResult> RemoveBrokerStatement(
        Guid accountId, Guid statementId, BrokerStatementService service, CancellationToken cancellationToken)
    {
        var removed = await service.Remove(accountId, statementId, cancellationToken);

        return removed.ToHttpResult();
    }
}
