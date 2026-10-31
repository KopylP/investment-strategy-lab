using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record TaxpayerAccountHistory(Guid Id, string Name, string Broker, ITaxPolicy Policy, IReadOnlyList<TaxEvent> Events);
