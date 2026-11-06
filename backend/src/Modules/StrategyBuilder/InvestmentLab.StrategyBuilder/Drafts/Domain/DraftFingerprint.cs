using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class DraftFingerprint
{
    public static string Of(GeneratedRules rules, DraftSetup setup, IReadOnlyList<DraftReport> reports, TrialExperiment? experiment = null)
    {
        var whole = new JsonObject
        {
            ["rules"] = JsonNode.Parse(rules.Json),
            ["setup"] = JsonSerializer.SerializeToNode(setup, JsonSerializerOptions.Web),
            ["reports"] = JsonSerializer.SerializeToNode(reports, JsonSerializerOptions.Web),
        };
        if (experiment is not null)
            whole["experiment"] = JsonSerializer.SerializeToNode(experiment, JsonSerializerOptions.Web);

        var bytes = Encoding.UTF8.GetBytes(CanonicalJson.Write(whole));

        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
