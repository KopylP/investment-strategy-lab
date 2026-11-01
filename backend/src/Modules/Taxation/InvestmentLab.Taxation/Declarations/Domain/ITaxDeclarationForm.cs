namespace InvestmentLab.Taxation.Declarations.Domain;

public interface ITaxDeclarationForm
{
    string Code { get; }

    string Title { get; }

    string Jurisdiction { get; }

    TaxDeclaration Fill(DeclarationFacts facts);
}
