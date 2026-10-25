using InvestmentLab.Api;
using InvestmentLab.BuildingBlocks.Modules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource(ResourceNames.Database);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
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
