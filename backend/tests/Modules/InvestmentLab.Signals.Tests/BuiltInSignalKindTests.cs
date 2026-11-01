using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class BuiltInSignalKindTests
{
    [Theory]
    [InlineData("vix:owner", BuiltInSignalKind.VolatilityIndex)]
    [InlineData("account-drawdown:account", BuiltInSignalKind.AccountDrawdown)]
    [InlineData("position-drawdown:account", BuiltInSignalKind.PositionDrawdown)]
    public void The_kind_of_a_built_in_signal_comes_from_its_key(string key, string kind)
    {
        BuiltInSignalKind.OfKey(key).ShouldBe(kind);
    }

    [Fact]
    public void A_built_in_key_keeps_the_stored_format_and_gives_back_its_kind()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        const string StoredId = "11111111222233334444555555555555";

        BuiltInSignalKind.VolatilityIndexKey(id).ShouldBe($"vix:{StoredId}");
        BuiltInSignalKind.AccountDrawdownKey(id).ShouldBe($"account-drawdown:{StoredId}");
        BuiltInSignalKind.PositionDrawdownKey(id).ShouldBe($"position-drawdown:{StoredId}");

        BuiltInSignalKind.OfKey(BuiltInSignalKind.VolatilityIndexKey(id)).ShouldBe(BuiltInSignalKind.VolatilityIndex);
        BuiltInSignalKind.OfKey(BuiltInSignalKind.AccountDrawdownKey(id)).ShouldBe(BuiltInSignalKind.AccountDrawdown);
        BuiltInSignalKind.OfKey(BuiltInSignalKind.PositionDrawdownKey(id)).ShouldBe(BuiltInSignalKind.PositionDrawdown);
    }

    [Fact]
    public void An_unknown_key_fails_instead_of_guessing_a_kind()
    {
        Should.Throw<ArgumentException>(() => BuiltInSignalKind.OfKey("new-thing:account"));
    }
}
