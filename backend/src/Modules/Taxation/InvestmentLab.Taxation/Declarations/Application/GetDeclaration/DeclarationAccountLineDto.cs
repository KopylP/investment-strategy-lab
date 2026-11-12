using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationAccountLineDto(
    string Name,
    string Broker,
    string Treatment,
    decimal Revenue,
    decimal Costs,
    decimal DividendsGross,
    decimal DividendsWithheld,
    Guid AccountId,
    bool TakesPit8C,
    Pit8CAmountsDto? Pit8C,
    string? Pit8CCoveredBy)
{
    public static DeclarationAccountLineDto From(DeclarationAccountLine declarationAccountLine) => new(
        Name: declarationAccountLine.Name,
        Broker: declarationAccountLine.Broker,
        Treatment: declarationAccountLine.Treatment,
        Revenue: declarationAccountLine.Revenue,
        Costs: declarationAccountLine.Costs,
        DividendsGross: declarationAccountLine.DividendsGross,
        DividendsWithheld: declarationAccountLine.DividendsWithheld,
        AccountId: declarationAccountLine.AccountId,
        TakesPit8C: declarationAccountLine.TakesPit8C,
        Pit8C: declarationAccountLine.Pit8C is { } pit8C ? Pit8CAmountsDto.From(pit8C) : null,
        Pit8CCoveredBy: declarationAccountLine.Pit8CCoveredBy);
}
