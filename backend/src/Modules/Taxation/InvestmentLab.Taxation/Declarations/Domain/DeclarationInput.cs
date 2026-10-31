using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationInput(int Year, IReadOnlyList<DeclarationAccount> Accounts, IReadOnlyList<LossYear> EarlierLosses, Func<Guid, string?> CountryOf);
