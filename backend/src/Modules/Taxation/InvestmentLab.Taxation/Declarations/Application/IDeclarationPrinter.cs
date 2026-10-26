using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

/// <summary>Prints a filled return on the official form of its <see cref="Form"/>; a form without a printer has no PDF.</summary>
public interface IDeclarationPrinter
{
    string Form { get; }

    byte[] Print(TaxDeclaration declaration);
}
