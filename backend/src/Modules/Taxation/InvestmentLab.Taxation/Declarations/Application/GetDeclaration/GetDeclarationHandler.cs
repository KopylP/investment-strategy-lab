using InvestmentLab.Taxation.Declarations.Application.Shared;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed class GetDeclarationHandler(DeclarationFiller filler) : IHandler
{
    public async Task<Result<DeclarationDto>> Handle(int year, string code, CancellationToken cancellationToken)
    {
        var filled = await filler.FillAsync(year, code, cancellationToken);
        if (filled.IsFailure)
            return filled.Error;

        return new DeclarationDto(TaxDeclarationDto.From(filled.Value.Declaration), filled.Value.Warnings);
    }
}
