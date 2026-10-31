using System.Text.Json.Nodes;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineJob(string Status, JsonNode? Result, int? ErrorStatusCode, string? ErrorDetail);
