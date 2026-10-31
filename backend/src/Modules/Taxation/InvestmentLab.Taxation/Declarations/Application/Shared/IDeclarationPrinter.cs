using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.Shared;

public interface IDeclarationPrinter
{
    string Kind { get; }

    bool Prints(TaxDeclaration declaration);

    byte[] Print(TaxDeclaration declaration);
}
