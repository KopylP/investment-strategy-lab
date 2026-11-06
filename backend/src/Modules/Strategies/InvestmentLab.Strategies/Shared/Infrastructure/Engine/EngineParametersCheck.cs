using System.Text.Json;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineParametersCheck(IReadOnlyList<EngineParameterProblem> Problems, IReadOnlyList<string> SignalIds, int HistoryBars = 260);

public sealed record EngineParameterProblem(IReadOnlyList<JsonElement> Path, string Code, string Message);
