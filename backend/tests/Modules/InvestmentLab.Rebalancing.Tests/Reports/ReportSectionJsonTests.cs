using System.Text.Json;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Reports;

/// <summary>Sections are stored as jsonb (web defaults): reports made before strategies named their columns still read.</summary>
public sealed class ReportSectionJsonTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void A_decision_written_before_the_columns_existed_reads_without_them()
    {
        const string json = """
            {"marketIsFavorable":true,"investFraction":1,"marketReasons":[],"summary":"Обрано 1","targets":[],"warnings":[],
             "scores":[{"instrumentId":"5f0c7a52-3c55-4a43-9b8e-2a3f1c7d9e01","name":"VWCE","group":"World","price":100,"momentum":0.2,
                        "return1m":0.01,"return3m":0.05,"return12m":0.2,"volatility":0.15,"aboveMovingAverage":true,"rank":1,
                        "status":"selected","reason":"ранг 1"}]}
            """;

        var decision = JsonSerializer.Deserialize<SectionDecision>(json, Web)!;

        decision.Columns.ShouldBeNull();
        decision.ScoreTitle.ShouldBeNull();
        decision.Scores[0].Values.ShouldBeNull();
        decision.Scores[0].AboveMovingAverage.ShouldBe(true);
    }

    [Fact]
    public void The_strategy_columns_and_values_survive_the_round_trip_with_their_ids_as_written()
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"return1m":0.01,"ma200":false,"sector":"tech","pe":null}""")!;
        var decision = new SectionDecision(
            true, 1m, [], "Обрано 1", [], [new SectionScore(Guid.NewGuid(), "X", "G", null, null, null, null, null, null, null, 1, "selected", "ранг 1", values)], [],
            "Чому саме ці акції", "Акція", [new SectionColumn("return1m", "1 міс", "signedPercent", null, false, null, null)]);

        var json = JsonSerializer.Serialize(decision, Web);
        var read = JsonSerializer.Deserialize<SectionDecision>(json, Web)!;

        json.ShouldContain("\"return1m\":0.01");
        read.Columns!.Single().Id.ShouldBe("return1m");
        read.Scores[0].Values!["ma200"].GetBoolean().ShouldBeFalse();
        read.Scores[0].Values!["sector"].GetString().ShouldBe("tech");
        read.Scores[0].Values!["pe"].ValueKind.ShouldBe(JsonValueKind.Null);
        read.Scores[0].AboveMovingAverage.ShouldBeNull();
    }
}
