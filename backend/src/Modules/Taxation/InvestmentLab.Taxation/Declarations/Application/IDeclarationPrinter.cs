using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

/// <summary>Prints a filled return as a PDF of one <see cref="Kind"/>: "summary" (what to type where) or "form" (the official form).</summary>
public interface IDeclarationPrinter
{
    string Kind { get; }

    bool Prints(TaxDeclaration declaration);

    byte[] Print(TaxDeclaration declaration);
}
