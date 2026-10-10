using FixtureHub.Api;
using FixtureHub.Api.Endpoints;
using FixtureHub.Application;
using FixtureHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FixtureHub")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'FixtureHub'.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString)
    .AddApi();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(
    applyMigrations: app.Configuration.GetValue<bool>("Database:ApplyMigrations"),
    seed: app.Configuration.GetValue<bool>("Database:Seed"));

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapApiEndpoints();

app.Run();
