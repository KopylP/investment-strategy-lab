using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Tests.Localization;

public sealed class MessageLocalizerTests
{
    private static readonly MessageLocalizer Localizer = new(new Dictionary<string, string>
    {
        ["test.plain"] = "Просто текст",
        ["test.value"] = "Назва: {name}",
        ["test.percent"] = "{share:percent} і {change:signedPercent}",
        ["test.money"] = "{amount:money:currency}",
        ["test.count"] = "{count} {count:plural:дія|дії|дій}",
        ["test.outer"] = "Причина: {reason}",
        ["test.list"] = "{items}",
    });

    [Fact]
    public void A_text_that_is_not_a_message_is_shown_as_it_is()
    {
        Localizer.Render("старий запис без коду").ShouldBe("старий запис без коду");
        Localizer.Render("користувач написав: @ось").ShouldBe("користувач написав: @ось");
    }

    [Fact]
    public void An_unknown_code_stays_visible_instead_of_disappearing()
    {
        Localizer.Render("@test.no_such_code").ShouldBe("@test.no_such_code");
    }

    [Fact]
    public void Parameters_fill_the_template_keeping_the_users_letters()
    {
        Localizer.Render(Message.Of("test.plain")).ShouldBe("Просто текст");
        Localizer.Render(Message.Of("test.value", new { name = "Рахунок «Є»" })).ShouldBe("Назва: Рахунок «Є»");
    }

    [Fact]
    public void A_parameter_without_a_value_leaves_its_place_empty_whatever_its_format()
    {
        var withoutValues = Message.Of("test.percent", new { share = (decimal?)null, change = (decimal?)null });

        Localizer.Render(withoutValues).ShouldBe(" і ");
    }

    [Fact]
    public void Numbers_are_formatted_by_the_template()
    {
        Localizer.Render(Message.Of("test.percent", new { share = 0.25m, change = 0.281m })).ShouldBe("25% і +28.1%");
        Localizer.Render(Message.Of("test.money", new { amount = 1234.5m, currency = "EUR" })).ShouldBe("€1,234.50");
        Localizer.Render(Message.Of("test.money", new { amount = -2m, currency = "PLN" })).ShouldBe("-zł 2.00");
    }

    [Theory]
    [InlineData(1, "1 дія")]
    [InlineData(3, "3 дії")]
    [InlineData(5, "5 дій")]
    [InlineData(11, "11 дій")]
    [InlineData(21, "21 дія")]
    public void Counted_words_follow_the_rules_of_the_language(int count, string expected)
    {
        Localizer.Render(Message.Of("test.count", new { count })).ShouldBe(expected);
    }

    [Fact]
    public void A_message_inside_a_message_is_rendered_too()
    {
        var inner = Message.Of("test.value", new { name = "ETF" });

        Localizer.Render(Message.Of("test.outer", new { reason = inner })).ShouldBe("Причина: Назва: ETF");
    }

    [Fact]
    public void A_list_parameter_renders_each_of_its_messages()
    {
        string[] items = [Message.Of("test.plain"), "без коду"];

        Localizer.Render(Message.Of("test.list", new { items })).ShouldBe("Просто текст без коду");
    }

    [Fact]
    public void A_missing_parameter_leaves_an_empty_place()
    {
        Localizer.Render(Message.Of("test.value")).ShouldBe("Назва: ");
    }

    [Fact]
    public void The_global_catalog_is_embedded()
    {
        MessageLocalizer.Default.Render(Message.Of("month.1")).ShouldBe("січень");
    }
}
