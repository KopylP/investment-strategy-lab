using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.RemovePit8C;

public sealed class RemovePit8CHandler(IPit8CRepository pit8C)
{
    public async Task<Result> Handle(int year, Guid accountId, CancellationToken cancellationToken)
    {
        var ofYear = await pit8C.ForYearAsync(year, cancellationToken);
        if (ofYear.FirstOrDefault(statement => statement.AccountId == accountId) is not { } statement)
            return Error.NotFound("tax.pit8c_not_found", $"No PIT-8C amounts for {year}.");

        pit8C.Remove(statement);
        await pit8C.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
