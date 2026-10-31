namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationAccountLine(
    string Name, string Broker, string Treatment, decimal Revenue, decimal Costs, decimal DividendsGross, decimal DividendsWithheld,
    Guid AccountId = default, bool TakesPit8C = false, Pit8CAmounts? Pit8C = null, string? Pit8CCoveredBy = null);
