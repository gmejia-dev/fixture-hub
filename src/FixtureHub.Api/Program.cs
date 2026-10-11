using FixtureHub.Api;
using FixtureHub.Api.Endpoints;
using FixtureHub.Api.Observability;
using FixtureHub.Application;
using FixtureHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FixtureHub")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'FixtureHub'.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString)
    .AddApi(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(
    applyMigrations: app.Configuration.GetValue<bool>("Database:ApplyMigrations"),
    seed: app.Configuration.GetValue<bool>("Database:Seed"));

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseMiddleware<CorrelationMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapApiEndpoints();

app.Run();
