namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record UploadBrokerStatementCommand(string FileName, string ContentBase64);
