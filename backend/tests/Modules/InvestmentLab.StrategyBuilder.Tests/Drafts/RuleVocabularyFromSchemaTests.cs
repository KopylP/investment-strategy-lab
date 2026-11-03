using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class RuleVocabularyFromSchemaTests
{
    private static RuleVocabulary Vocabulary => EngineSchema.Vocabulary;

    [Fact]
    public void The_engine_schema_gives_every_indicator_with_its_fields()
    {
        var sma = Vocabulary.Indicator("sma").ShouldNotBeNull();
        var days = sma.Fields.Single(field => field.Name == "days");

        days.Kind.ShouldBe(FieldKind.Integer);
        days.Minimum.ShouldBe(2);
        days.Maximum.ShouldBe(400);
        sma.Fields.Single(field => field.Name == "of").Kind.ShouldBe(FieldKind.Series);
        Vocabulary.Indicator("blendedMomentum")!.Fields.Select(field => field.Name).ShouldBe(["of"]);
        Vocabulary.Indicators.Count.ShouldBe(11);
    }

    [Fact]
    public void Fractions_and_choices_are_recognised()
    {
        var share = Vocabulary.BlockFields["investWhenUnfavorable"];
        var operatorField = Vocabulary.Condition("compare")!.Fields.Single(field => field.Name == "operator");

        share.Kind.ShouldBe(FieldKind.Fraction);
        share.Minimum.ShouldBe(0);
        share.Maximum.ShouldBe(1);
        operatorField.Kind.ShouldBe(FieldKind.Choice);
        operatorField.ChoiceLabels.Keys.Order().ShouldBe(["gt", "gte", "lt", "lte"]);
    }

    [Fact]
    public void Labels_hints_and_phrases_are_message_codes()
    {
        var sma = Vocabulary.Indicator("sma")!;

        sma.Label.ShouldStartWith("@rule_based.indicator.sma.label");
        sma.Hint.ShouldStartWith("@rule_based.indicator.sma.hint");
        sma.Phrase.ShouldStartWith("@rule_based.indicator.sma.phrase");
        sma.ColumnFormat.ShouldBe("number");
    }

    [Fact]
    public void Every_condition_kind_is_known()
    {
        Vocabulary.Conditions.Select(condition => condition.Code).Order().ShouldBe(["all", "any", "between", "compare", "crossed", "not", "signal"]);
        Vocabulary.Condition("crossed")!.Fields.Single(field => field.Name == "other").Kind.ShouldBe(FieldKind.Indicator);
    }
}
