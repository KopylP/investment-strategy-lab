using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

internal static class IntegrationEvents
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string TypeName<TEvent>() => typeof(TEvent).FullName ?? typeof(TEvent).Name;
}
