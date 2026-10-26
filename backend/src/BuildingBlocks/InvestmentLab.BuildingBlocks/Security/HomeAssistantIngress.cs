using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.BuildingBlocks.Security;

/// <summary>
/// The Home Assistant add-on's sidebar panel: the Supervisor proxies it to the ingress port (<c>HomeAssistant:IngressPort</c>)
/// once Home Assistant has signed the user in, and says who it is in <c>X-Remote-User-*</c> headers (stripping any the
/// browser sent). Those headers, and <c>X-Ingress-Path</c>, are believed only on that port and from the Supervisor's
/// address: other add-ons share its Docker network and could send them too.
/// </summary>
public static class HomeAssistantIngress
{
    public const string PathHeader = "X-Ingress-Path";
    public const string UserIdHeader = "X-Remote-User-Id";
    public const string UserNameHeader = "X-Remote-User-Name";

    /// <summary>The Supervisor's fixed address on the add-ons' network.</summary>
    private static readonly IPAddress DefaultSupervisor = IPAddress.Parse("172.30.32.2");

    extension(HttpContext context)
    {
        /// <summary>True for a request the Supervisor proxied to the ingress port; never outside the add-on.</summary>
        public bool IsFromHomeAssistantIngress()
        {
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            if (!int.TryParse(configuration["HomeAssistant:IngressPort"], out var port) || context.Connection.LocalPort != port)
                return false;

            var supervisor = IPAddress.TryParse(configuration["HomeAssistant:SupervisorAddress"], out var configured) ? configured : DefaultSupervisor;
            // Kestrel listens on "*" (IPv6 and IPv4): an IPv4 peer arrives as ::ffff:172.30.32.2.
            return context.Connection.RemoteIpAddress is { } remote
                && (remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote).Equals(supervisor);
        }

        /// <summary>The Home Assistant user the Supervisor vouches for, or null outside its ingress.</summary>
        public string? HomeAssistantUserId() =>
            context.Request.Headers[UserIdHeader].ToString() is { Length: > 0 } userId && context.IsFromHomeAssistantIngress() ? userId : null;
    }
}
