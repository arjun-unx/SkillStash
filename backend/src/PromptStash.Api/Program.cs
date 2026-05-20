using PromptStash.Api.Common.Extensions;
using PromptStash.Api.Common.Middleware;
using PromptStash.Api.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local secrets (gitignored). Copy appsettings.Secrets.json.example → appsettings.Secrets.json
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SkillStash.Api")
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {CorrelationId} {Message:lj}{NewLine}{Exception}"));

// Single composition root that wires Database, MediatR pipeline, FluentValidation,
// JWT auth, app services and API infrastructure (Controllers, Swagger, CORS, RateLimiter, Health).
builder.Services.AddPromptStash(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPromptStashHealth();

if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        await DbInitializer.InitializeAsync(app.Services, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed");
    }
}

app.Run();

public partial class Program;
