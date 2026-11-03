using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record NodeDto(string Id, string Kind, string Block, IReadOnlyDictionary<string, JsonElement> Parameters);
