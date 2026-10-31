namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record BrokerStatementDto(Guid Id, string Broker, string FileName, DateTimeOffset UploadedAt, DateOnly From, DateOnly To, int Sales, int Dividends,
    IReadOnlyList<string> Warnings);
