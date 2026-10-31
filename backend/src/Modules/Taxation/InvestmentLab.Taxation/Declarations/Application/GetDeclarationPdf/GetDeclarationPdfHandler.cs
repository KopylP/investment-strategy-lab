using InvestmentLab.Taxation.Declarations.Application.GetDeclaration;
using InvestmentLab.Taxation.Declarations.Application.Shared;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclarationPdf;

public sealed class GetDeclarationPdfHandler(GetDeclarationHandler declarations, IEnumerable<IDeclarationPrinter> printers)
{
    public async Task<Result<(byte[] Content, string FileName)>> Handle(int year, string code, string kind, CancellationToken cancellationToken)
    {
        var filled = await declarations.Handle(year, code, cancellationToken);
        if (filled.IsFailure)
            return filled.Error;

        var declaration = filled.Value.Declaration;
        if (printers.FirstOrDefault(printer => printer.Kind == kind && printer.Prints(declaration)) is not { } printer)
            return Error.NotFound("tax.form_not_printable", $"{declaration.Form} has no {kind} PDF.");

        var fileName = $"{declaration.Form}-{year}-{(kind == "form" ? "formularz" : "sciaga")}.pdf";

        return (printer.Print(declaration), fileName);
    }
}
