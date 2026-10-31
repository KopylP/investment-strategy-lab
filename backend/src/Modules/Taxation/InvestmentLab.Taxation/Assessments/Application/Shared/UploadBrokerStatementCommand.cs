namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record UploadBrokerStatementCommand(string FileName, string ContentBase64);
