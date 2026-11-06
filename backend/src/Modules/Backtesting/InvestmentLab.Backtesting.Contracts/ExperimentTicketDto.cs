namespace InvestmentLab.Backtesting.Contracts;

public sealed record ExperimentTicketDto(Guid? Id, string? ErrorCode = null, string? ErrorMessage = null);
