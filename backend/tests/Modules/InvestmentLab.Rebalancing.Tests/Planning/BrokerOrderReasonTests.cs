using System.Text.Json;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class BrokerOrderReasonTests
{
    private static BrokerOrder OrderWithReasons(params string[] reasons)
    {
        var allocations = reasons.Select(reason => new OrderAllocation(Guid.NewGuid(), OrderKind.Exit, 1m, reason)).ToList();

        return new BrokerOrder(OrderKind.Exit, Guid.NewGuid(), reasons.Length, 10m, allocations);
    }

    [Fact]
    public void One_reason_stays_the_message_it_was()
    {
        var reason = Message.Of("report.reason.exit_dropped");

        OrderWithReasons(reason, reason).Reason.ShouldBe(reason);
    }

    [Fact]
    public void Different_reasons_travel_as_parameters_of_one_message()
    {
        var first = Message.Of("report.reason.exit_signal", new { reason = "a" });
        var second = Message.Of("report.reason.exit_signal", new { reason = "b" });

        var reason = OrderWithReasons(first, second).Reason;

        const string code = "@report.reason.several ";
        reason.ShouldStartWith(code);

        using var parameters = JsonDocument.Parse(reason[code.Length..]);
        var reasons = parameters.RootElement.GetProperty("reasons").EnumerateArray().Select(item => item.GetString());
        reasons.ShouldBe([first, second]);
    }
}
