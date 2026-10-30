namespace InvestmentLab.SharedKernel.Tests;

public sealed class AggregateRootTests
{
    private sealed record SomethingHappened(Guid Id) : IDomainEvent;

    private sealed class Sample(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoSomething() => Raise(new SomethingHappened(Id));
    }

    [Fact]
    public void Raised_events_are_collected_until_cleared()
    {
        var sample = new Sample(Guid.NewGuid());

        sample.DoSomething();

        sample.DomainEvents.ShouldHaveSingleItem().ShouldBe(new SomethingHappened(sample.Id));

        sample.ClearDomainEvents();
        sample.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = Guid.NewGuid();

        new Sample(id).ShouldBe(new Sample(id));
        new Sample(id).ShouldNotBe(new Sample(Guid.NewGuid()));
    }
}
