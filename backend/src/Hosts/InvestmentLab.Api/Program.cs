using InvestmentLab.Api;
using InvestmentLab.BuildingBlocks.Modules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource(ResourceNames.Database);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // In development the cause of a 500 is shown, so a failing screen or test says what broke.
    if (context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
        && context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error is { } error)
        context.ProblemDetails.Detail = $"{error.GetType().Name}: {error.Message} {error.InnerException?.Message}";
});
builder.AddModules(Modules.All());

var app = builder.Build();

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

app.Run();

public partial class Program;
