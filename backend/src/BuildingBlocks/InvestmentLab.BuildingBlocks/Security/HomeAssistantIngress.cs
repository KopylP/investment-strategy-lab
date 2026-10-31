using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.BuildingBlocks.Security;

public static class HomeAssistantIngress
{
    public const string PathHeader = "X-Ingress-Path";
    public const string UserIdHeader = "X-Remote-User-Id";
    public const string UserNameHeader = "X-Remote-User-Name";

    private static readonly IPAddress DefaultSupervisor = IPAddress.Parse("172.30.32.2");

    extension(HttpContext context)
    {
        public bool IsFromHomeAssistantIngress()
        {
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            if (!int.TryParse(configuration["HomeAssistant:IngressPort"], out var port) || context.Connection.LocalPort != port)
                return false;

            var supervisor = IPAddress.TryParse(configuration["HomeAssistant:SupervisorAddress"], out var configured) ? configured : DefaultSupervisor;

            return context.Connection.RemoteIpAddress is { } remote
                && (remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote).Equals(supervisor);
        }

        public string? HomeAssistantUserId() =>
            context.Request.Headers[UserIdHeader].ToString() is { Length: > 0 } userId && context.IsFromHomeAssistantIngress() ? userId : null;
    }
}
