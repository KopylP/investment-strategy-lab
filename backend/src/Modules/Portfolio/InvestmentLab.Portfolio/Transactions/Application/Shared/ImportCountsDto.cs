using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record ImportCountsDto(
    int New,
    int AlreadyImported,
    int Differs,
    int MatchesExisting,
    int OutOfScope,
    int Unsupported,
    int NeedsMapping,
    int ToRecord,
    int ToReverse);
