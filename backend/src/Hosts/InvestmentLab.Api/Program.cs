using InvestmentLab.Api;
using InvestmentLab.Api.Hosting;
using InvestmentLab.BuildingBlocks.Modules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource(ResourceNames.Database);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
        && context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error is { } error)
        context.ProblemDetails.Detail = $"{error.GetType().Name}: {error.Message} {error.InnerException?.Message}";
});
var modules = Modules.All();
builder.AddModules(modules);
builder.AddSelfHosting();
builder.AddMcpAccess(modules);

var app = builder.Build();

app.UseMcpForwarding();
app.UseSelfHosting();

app.UseRouting();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapDefaultEndpoints();
app.MapModules();
app.MapMcpAccess();
app.MapFrontend();

app.Run();
