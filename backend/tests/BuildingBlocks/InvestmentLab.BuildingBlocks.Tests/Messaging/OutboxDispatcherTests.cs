using System.Reflection;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace InvestmentLab.BuildingBlocks.Tests.Messaging;

public sealed class OutboxDispatcherTests : IAsyncDisposable
{
    private const string EventType = "Thing";
    private const string Failing = "fails";
    private const int BatchSize = 50;

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 12, 20, 0, 0, TimeSpan.Zero));
    private readonly CountingDelivery _delivery = new();
    private readonly RecordingLogger _logger = new();
    private readonly ServiceProvider _services;
    private readonly OutboxDispatcher _dispatcher;

    public OutboxDispatcherTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var slowModuleDatabaseName = Guid.NewGuid().ToString();
        _services = new ServiceCollection()
            .AddDbContext<OutboxDbContext>(options => options.UseInMemoryDatabase(databaseName))
            .AddDbContext<SlowModuleDbContext>(options => options.UseInMemoryDatabase(slowModuleDatabaseName))
            .BuildServiceProvider();

        _dispatcher = new OutboxDispatcher(
            contexts: [new ModuleDbContextType(typeof(OutboxDbContext))],
            deliveries: [_delivery],
            scopes: _services.GetRequiredService<IServiceScopeFactory>(),
            clock: _clock,
            logger: _logger);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "things";

        protected override Assembly MappingsAssembly => typeof(OutboxDispatcherTests).Assembly;

        protected override bool IsMappedHere(Type configuration) => false;
    }

    private sealed class SlowModuleDbContext(DbContextOptions<SlowModuleDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "slow";

        protected override Assembly MappingsAssembly => typeof(OutboxDispatcherTests).Assembly;

        protected override bool IsMappedHere(Type configuration) => false;
    }

    private sealed class CountingDelivery : IIntegrationEventDelivery
    {
        public List<string> Delivered { get; } = [];

        public string EventType => OutboxDispatcherTests.EventType;

        public Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken)
        {
            Delivered.Add(payload);
            if (payload.StartsWith(Failing, StringComparison.Ordinal))
                throw new InvalidOperationException("Home Assistant is not reachable");

            return Task.CompletedTask;
        }
    }

    private sealed class GatedDelivery(string eventType) : IIntegrationEventDelivery
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string EventType => eventType;

        public void Open() => _gate.TrySetResult();

        public async Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            Delivered.TrySetResult();
        }
    }

    private sealed class RecordingLogger : ILogger<OutboxDispatcher>
    {
        public List<(LogLevel Level, string Text)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private Task<OutboxMessage> AddMessageAsync(string payload) => AddMessageAsync<OutboxDbContext>(EventType, payload);

    private async Task<OutboxMessage> AddMessageAsync<TContext>(string eventType, string payload)
        where TContext : DbContext
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var message = OutboxMessage.For(eventType, payload, _clock.GetUtcNow());
        context.Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _clock.Advance(TimeSpan.FromMilliseconds(1));

        return message;
    }

    private async Task<OutboxMessage> StoredAsync(Guid id)
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        return await context.Set<OutboxMessage>().SingleAsync(message => message.Id == id, TestContext.Current.CancellationToken);
    }

    private Task<int> DispatchAsync() =>
        _dispatcher.DispatchBatchAsync(new ModuleDbContextType(typeof(OutboxDbContext)), TestContext.Current.CancellationToken);

    private int DeliveriesOf(string payload) => _delivery.Delivered.Count(each => each == payload);

    [Fact]
    public async Task A_failed_message_is_not_tried_again_before_its_retry_delay_has_passed()
    {
        await AddMessageAsync(Failing);
        await DispatchAsync();

        _clock.Advance(TimeSpan.FromSeconds(9));
        await DispatchAsync();
        DeliveriesOf(Failing).ShouldBe(1);

        _clock.Advance(TimeSpan.FromSeconds(1));
        await DispatchAsync();
        DeliveriesOf(Failing).ShouldBe(2);
    }

    [Fact]
    public async Task Messages_waiting_for_a_retry_do_not_hold_back_a_newer_message()
    {
        for (var index = 0; index < BatchSize; index++)
            await AddMessageAsync($"{Failing} {index}");
        await DispatchAsync();
        var newer = await AddMessageAsync("newer");

        await DispatchAsync();

        DeliveriesOf("newer").ShouldBe(1);
        (await StoredAsync(newer.Id)).ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_message_that_keeps_failing_is_given_up_and_logged_as_an_error_with_its_type_and_id()
    {
        var message = await AddMessageAsync(Failing);

        for (var attempt = 0; attempt < OutboxMessage.MaxAttempts + 3; attempt++)
        {
            await DispatchAsync();
            _clock.Advance(TimeSpan.FromHours(2));
        }

        DeliveriesOf(Failing).ShouldBe(OutboxMessage.MaxAttempts);
        (await StoredAsync(message.Id)).IsAbandoned.ShouldBeTrue();
        var (_, errorText) = _logger.Entries.Where(entry => entry.Level == LogLevel.Error).ShouldHaveSingleItem();
        errorText.ShouldContain(EventType);
        errorText.ShouldContain(message.Id.ToString());
    }

    [Fact]
    public async Task A_failure_before_the_last_attempt_is_only_a_warning()
    {
        await AddMessageAsync(Failing);

        await DispatchAsync();

        _logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task A_module_whose_handler_is_slow_does_not_hold_back_the_events_of_another_module()
    {
        var slow = new GatedDelivery("SlowThing");
        var prompt = new GatedDelivery("PromptThing");
        prompt.Open();
        await AddMessageAsync<SlowModuleDbContext>(slow.EventType, "slow");
        await AddMessageAsync<OutboxDbContext>(prompt.EventType, "prompt");
        using var dispatcher = new OutboxDispatcher(
            contexts: [new ModuleDbContextType(typeof(SlowModuleDbContext)), new ModuleDbContextType(typeof(OutboxDbContext))],
            deliveries: [slow, prompt],
            scopes: _services.GetRequiredService<IServiceScopeFactory>(),
            clock: _clock,
            logger: _logger);

        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        await prompt.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        slow.Delivered.Task.IsCompleted.ShouldBeFalse();
        slow.Open();
        await dispatcher.StopAsync(TestContext.Current.CancellationToken);
    }
}
