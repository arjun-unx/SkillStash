using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using HealthChecks.UI.Client;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PromptStash.Api.Common.Behaviors;
using PromptStash.Api.Common.Settings;
using PromptStash.Api.Data;
using PromptStash.Api.Services;
using PromptStash.Api.Services.Auth;
using PromptStash.Api.Services.Messaging;
using PromptStash.Api.Services.Repositories;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Single composition root: registers Database, MediatR + behaviors, FluentValidation,
    /// Auth (JWT), Services (repositories, JWT, hashing, email, service-bus, consumers, handlers),
    /// Controllers, Swagger, CORS, Rate-limiting and Health checks.
    /// </summary>
    public static IServiceCollection AddPromptStash(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddDatabase(configuration);
        services.AddMediatRPipeline(assembly);
        services.AddJwtAuth(configuration);
        services.AddAppServices(configuration);
        services.AddApiInfrastructure(configuration);

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Postgres"),
                npg => npg.MigrationsHistoryTable("__ef_migrations_history"));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });
    }

    private static void AddMediatRPipeline(this IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
    }

    private static void AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret) && o.Secret.Length >= 32,
                "Jwt:Secret must be at least 32 characters.")
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Jwt configuration is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync("""{"error":"unauthorized"}""");
                    }
                };
            });
        services.AddAuthorization();
    }

    private static void AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
        services.Configure<TrendingOptions>(configuration.GetSection(TrendingOptions.SectionName));

        services.AddMemoryCache();

        // Cross-cutting
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Auth services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Repositories
        services.AddScoped<ISkillBookmarkRepository, SkillBookmarkRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();

        // Trending (GitHub SKILL.md aggregation)
        services.AddHttpClient("trending", (sp, c) =>
        {
            c.DefaultRequestHeaders.UserAgent.ParseAdd("SkillStash/1.0 (+https://github.com/skillstash)");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            c.Timeout = TimeSpan.FromSeconds(120);
            var token = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TrendingOptions>>().Value.GitHubToken;
            if (!string.IsNullOrWhiteSpace(token))
                c.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        });
        services.AddScoped<ITrendingSkillFetcher, GitHubSkillsTrendingFetcher>();
        services.AddScoped<ITrendingSkillRepository, TrendingSkillRepository>();
        services.AddScoped<ITrendingSkillSyncService, TrendingSkillSyncService>();
        services.AddHostedService<TrendingSkillSyncBackgroundService>();

        // Email
        services.AddSingleton<IEmailService, SmtpEmailService>();

        // Integration event dispatcher + handlers
        services.AddSingleton<EventDispatcher>();
        services.AddScoped<IIntegrationEventHandler, UserRegisteredHandler>();
        services.AddScoped<IIntegrationEventHandler, SkillPublishedHandler>();

        // Service Bus publisher + consumer
        var bus = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>() ?? new();
        var worker = configuration.GetSection(WorkerOptions.SectionName).Get<WorkerOptions>() ?? new();

        if (bus.UseAzure)
        {
            services.AddSingleton<IServiceBusPublisher, AzureServiceBusPublisher>();
            if (worker.HostInProcess) services.AddHostedService<AzureServiceBusConsumer>();
        }
        else
        {
            services.AddSingleton<InMemoryServiceBus>();
            services.AddSingleton<IServiceBusPublisher>(sp => sp.GetRequiredService<InMemoryServiceBus>());
            if (worker.HostInProcess) services.AddHostedService<InMemoryBusConsumer>();
        }
    }

    private static void AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        services.AddEndpointsApiExplorer();

        services.AddCors(o => o.AddPolicy("frontend", policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                          ?? ["http://localhost:4200"];
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SkillStash API",
                Version = "v1",
                Description = "ASP.NET Core API for SkillStash — discover, share, and sync agent skills (Claude, GPT, Gemini, Cursor)."
            });
            var jwt = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste a JWT token (without the 'Bearer ' prefix).",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            c.AddSecurityDefinition("Bearer", jwt);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwt] = Array.Empty<string>() });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0
                    }));
        });

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Postgres") ?? string.Empty, name: "postgres");
    }

    public static WebApplication MapPromptStashHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        return app;
    }
}
