namespace InvestmentLab.SharedKernel.Tests;

public sealed class UserIdTests
{
    [Fact]
    public void New_creates_distinct_ids()
    {
        UserId.New().ShouldNotBe(UserId.New());
    }

    [Fact]
    public void Of_rejects_empty_guid()
    {
        Should.Throw<ArgumentException>(() => UserId.Of(Guid.Empty));
    }

    [Fact]
    public void Of_keeps_value()
    {
        var guid = Guid.NewGuid();

        UserId.Of(guid).Value.ShouldBe(guid);
    }
}
