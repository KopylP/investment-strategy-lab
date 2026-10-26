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

        statements.MapGet("", async (Guid accountId, BrokerStatementService service, CancellationToken ct) => (await service.List(accountId, ct)).ToHttpResult())
            .WithName("ListBrokerStatements");
        statements.MapPost("", async (Guid accountId, UploadBrokerStatementCommand body, BrokerStatementService service, CancellationToken ct) =>
                (await service.Upload(accountId, body, ct)).ToHttpResult())
            .WithName("UploadBrokerStatement");
        statements.MapDelete("/{statementId:guid}", async (Guid accountId, Guid statementId, BrokerStatementService service, CancellationToken ct) =>
                (await service.Remove(accountId, statementId, ct)).ToHttpResult())
            .WithName("RemoveBrokerStatement");

        return group;
    }
}
