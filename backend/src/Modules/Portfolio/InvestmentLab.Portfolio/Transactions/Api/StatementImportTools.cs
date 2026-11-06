using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.Portfolio.Transactions.Application.ConfigureStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ConfirmStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.GetStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ListStatementImports;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Application.UndoStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.UploadStatement;
using ModelContextProtocol.Server;

namespace InvestmentLab.Portfolio.Transactions.Api;

[McpServerToolType]
internal static class StatementImportTools
{
    [McpServerTool(Name = "list_statement_imports", ReadOnly = true)]
    [Description("An account's broker statement imports.")]
    public static Task<IReadOnlyList<StatementImportSummaryDto>> ListStatementImports(
        Guid accountId,
        ListStatementImportsHandler handler,
        CancellationToken cancellationToken) =>
        handler.Handle(accountId, cancellationToken);

    [McpServerTool(Name = "upload_statement")]
    [Description("Reads a broker statement and prepares an import; nothing is recorded until confirm_statement_import.")]
    public static async Task<StatementImportDto> UploadStatement(
        Guid accountId,
        string fileName,
        [Description("The statement file's text, as exported by the broker.")] string content,
        UploadStatementHandler handler,
        CancellationToken cancellationToken)
    {
        var uploaded = await handler.Handle(accountId, fileName, content, cancellationToken);

        return uploaded.ValueOrThrow();
    }

    [McpServerTool(Name = "get_statement_import", ReadOnly = true)]
    [Description("A prepared statement import: what it will record, what needs a choice and its warnings.")]
    public static async Task<StatementImportDto> GetStatementImport(
        Guid accountId,
        Guid importId,
        GetStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var statementImport = await handler.Handle(accountId, importId, cancellationToken);

        return statementImport.ValueOrThrow();
    }

    [McpServerTool(Name = "configure_statement_import")]
    [Description("Chooses how a prepared import is applied: mode, start date, the sub-portfolio for cash and ticker mappings.")]
    public static async Task<StatementImportDto> ConfigureStatementImport(
        Guid accountId,
        Guid importId,
        ConfigureImportCommand choices,
        ConfigureStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var configured = await handler.Handle(accountId, importId, choices, cancellationToken);

        return configured.ValueOrThrow();
    }

    [McpServerTool(Name = "confirm_statement_import", Destructive = true)]
    [Description("Records the prepared import's transactions. Review it with get_statement_import and ask the user first.")]
    public static async Task<StatementImportDto> ConfirmStatementImport(
        Guid accountId,
        Guid importId,
        ConfirmStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var confirmed = await handler.Handle(accountId, importId, cancellationToken);

        return confirmed.ValueOrThrow();
    }

    [McpServerTool(Name = "undo_statement_import", Destructive = true)]
    [Description("Takes back the transactions a confirmed import recorded.")]
    public static async Task<StatementImportDto> UndoStatementImport(
        Guid accountId,
        Guid importId,
        UndoStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var undone = await handler.Handle(accountId, importId, cancellationToken);

        return undone.ValueOrThrow();
    }
}
