using InvestmentLab.BuildingBlocks.Security;
using Microsoft.AspNetCore.DataProtection;

namespace InvestmentLab.Api.Hosting;

public static class SelfHosting
{
    public static void AddSelfHosting(this WebApplicationBuilder builder)
    {
        if (builder.Configuration["InvestmentLab:DataDirectory"] is { Length: > 0 } dataDirectory)
            builder.Services.AddDataProtection()
                .SetApplicationName("InvestmentLab")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
    }

    public static void UseSelfHosting(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("HomeAssistant:IngressOnly"))
            app.Use((context, next) =>
            {
                if (context.IsFromHomeAssistantIngress() || McpAccess.IsMcpRequest(context))
                    return next(context);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            });

        if (app.Configuration["InvestmentLab:PathBase"] is { Length: > 1 } configured)
        {
            var pathBase = configured.TrimEnd('/');

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

        app.Use((context, next) =>
        {
            if (context.Request.Headers[HomeAssistantIngress.PathHeader].ToString() is { Length: > 1 } ingress
                && context.IsFromHomeAssistantIngress())
                context.Request.PathBase = new PathString(ingress.TrimEnd('/')) + context.Request.PathBase;

            return next(context);
        });

        if (HasFrontend(app))
            app.UseStaticFiles();
    }

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
                await context.Response.WriteAsync(
                    html.Replace("<base href=\"/\">", $"<base href=\"{context.Request.PathBase}/\">"),
                    context.RequestAborted);
            })
            .AllowAnonymous();
    }

    private static bool HasFrontend(WebApplication app) =>
        app.Environment.WebRootPath is { } root && File.Exists(Path.Combine(root, "index.html"));
}
