using InvestmentLab.BuildingBlocks.Security;
using Microsoft.AspNetCore.DataProtection;

namespace InvestmentLab.Api.Hosting;

/// <summary>
/// Running without Aspire (the Home Assistant add-on, a VPS): the API serves the built Angular app from wwwroot, under
/// a configurable path base (<c>InvestmentLab:PathBase</c>, e.g. <c>/investlab</c>) or the path Home Assistant Ingress
/// announces in <c>X-Ingress-Path</c> (only when the Supervisor sends it); cookie keys live in <c>InvestmentLab:DataDirectory</c> so sign-ins survive restarts.
/// </summary>
public static class SelfHosting
{
    public static void AddSelfHosting(this WebApplicationBuilder builder)
    {
        if (builder.Configuration["InvestmentLab:DataDirectory"] is { Length: > 0 } dataDirectory)
            builder.Services.AddDataProtection()
                .SetApplicationName("InvestmentLab")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
    }

    /// <summary>Path base first (before routing), then the app's static files.</summary>
    public static void UseSelfHosting(this WebApplication app)
    {
        if (app.Configuration["InvestmentLab:PathBase"] is { Length: > 1 } configured)
        {
            var pathBase = configured.TrimEnd('/');
            // "http://homeassistant.local:8099/" lands on the app instead of a 404.
            app.Use((context, next) =>
            {
                if (context.Request.Path == "/" && !context.IsFromHomeAssistantIngress())
                {
                    context.Response.Redirect($"{pathBase}/");
                    return Task.CompletedTask;
                }
                return next(context);
            });
            app.UsePathBase(pathBase);
        }

        // Home Assistant Ingress strips its prefix and tells it in a header; links must keep it.
        app.Use((context, next) =>
        {
            if (context.Request.Headers[HomeAssistantIngress.PathHeader].ToString() is { Length: > 1 } ingress && context.IsFromHomeAssistantIngress())
                context.Request.PathBase = new PathString(ingress.TrimEnd('/')) + context.Request.PathBase;
            return next(context);
        });

        if (HasFrontend(app))
            app.UseStaticFiles();
    }

    /// <summary>Every non-API path is the Angular app; its &lt;base href&gt; follows the path the request came through.</summary>
    public static void MapFrontend(this WebApplication app)
    {
        if (!HasFrontend(app))
            return;

        var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
        app.MapFallback(async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                var html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = "no-cache";
                await context.Response.WriteAsync(html.Replace("<base href=\"/\">", $"<base href=\"{context.Request.PathBase}/\">"), context.RequestAborted);
            })
            .AllowAnonymous();
    }

    private static bool HasFrontend(WebApplication app) =>
        app.Environment.WebRootPath is { } root && File.Exists(Path.Combine(root, "index.html"));
}
