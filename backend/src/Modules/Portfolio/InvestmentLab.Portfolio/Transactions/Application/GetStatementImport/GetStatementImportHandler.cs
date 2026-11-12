using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Transactions.Application.GetStatementImport;

public sealed class GetStatementImportHandler(StatementImportLoader loader, StatementPreview preview) : IHandler
{
    public async Task<Result<StatementImportDto>> Handle(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        return await preview.ShowAsync(loaded.Value.Account, loaded.Value.Import, cancellationToken);
    }
}
