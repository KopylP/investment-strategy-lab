using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.ListAccounts;
using InvestmentLab.Portfolio.Contracts.Accounts;
using ModelContextProtocol.Server;

namespace InvestmentLab.Portfolio.Accounts.Api;

[McpServerToolType]
internal static class AccountTools
{
    [McpServerTool(Name = "list_accounts", ReadOnly = true)]
    [Description("The user's accounts with their sub-portfolios and ids.")]
    public static Task<IReadOnlyList<AccountSetupDto>> ListAccounts(ListAccountsHandler handler, CancellationToken cancellationToken) =>
        handler.Handle(cancellationToken);

    [McpServerTool(Name = "get_account", ReadOnly = true)]
    [Description("One account with its sub-portfolios, strategies and target shares.")]
    public static async Task<AccountSetupDto> GetAccount(Guid accountId, GetAccountHandler handler, CancellationToken cancellationToken)
    {
        var account = await handler.Handle(accountId, cancellationToken);

        return account.ValueOrThrow();
    }
}
