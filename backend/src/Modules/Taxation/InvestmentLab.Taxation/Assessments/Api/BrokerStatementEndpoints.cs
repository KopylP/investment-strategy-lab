using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application.ListBrokerStatements;
using InvestmentLab.Taxation.Assessments.Application.RemoveBrokerStatement;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Application.UploadBrokerStatement;
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
        Guid accountId, ListBrokerStatementsHandler handler, CancellationToken cancellationToken)
    {
        var statements = await handler.Handle(accountId, cancellationToken);

        return statements.ToHttpResult();
    }

    private static async Task<IResult> UploadBrokerStatement(
        Guid accountId, UploadBrokerStatementCommand body, UploadBrokerStatementHandler handler, CancellationToken cancellationToken)
    {
        var uploaded = await handler.Handle(accountId, body, cancellationToken);

        return uploaded.ToHttpResult();
    }

    private static async Task<IResult> RemoveBrokerStatement(
        Guid accountId, Guid statementId, RemoveBrokerStatementHandler handler, CancellationToken cancellationToken)
    {
        var removed = await handler.Handle(accountId, statementId, cancellationToken);

        return removed.ToHttpResult();
    }
}
