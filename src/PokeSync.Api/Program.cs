using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using PokeSync.Api.Contracts.Upsert.Validation;
using PokeSync.Api.HostedServices;
using PokeSync.Api.Middleware;
using PokeSync.Api.Options;
using PokeSync.Infrastructure.Data;
using PokeSync.Domain.Interfaces;
using PokeSync.Infrastructure.Services;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

//Logger
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter());
});


//DbContext
builder.Services.AddDbContext<PokeSyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));


// Services
builder.Services.AddMemoryCache();
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));

var securitySection = builder.Configuration.GetSection(SecurityOptions.SectionName);
var securityOptions = securitySection.Get<SecurityOptions>() ?? new SecurityOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(securityOptions.AllowedOrigins)      // Dev + Prod
            .WithMethods("GET", "POST", "OPTIONS")           // ce qui est prévu par l’US
            .WithHeaders("Content-Type", "Accept");          // pas de headers exotiques
        // Pas de AllowCredentials() => pas de cookies cross-site, plus safe
    });
});
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IUpsertService, UpsertService>();
builder.Services.AddScoped<IPokemonUpsertService, PokemonUpsertService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddHostedService<ConfigurationValidatorHostedService>();
builder.Services.AddHostedService<BootstrapService>();
builder.Services.AddHostedService<NightlySyncService>();

// Controllers + minimal setup
builder.Services.AddControllers();
// FluentValidation auto-validation + scan de l’assembly API
builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddValidatorsFromAssemblyContaining<PokemonUpsertItemDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");            // CORS pour Angular

// 1) CorrelationId -> met le CorrelationId dans le LogContext (via BeginScope dans ton middleware)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2) Serilog request logging (ajoute un log structuré pour chaque requête)
app.UseSerilogRequestLogging();

// 3) Token interne (après avoir posé CorrelationId, avant les endpoints)
app.UseMiddleware<InternalTokenMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();


// ---- AUTO-MIGRATION AT STARTUP ----
// Automatically applies pending EF Core migrations at application startup.
// Safe for Development or Staging environments.
// Controlled via "AutoMigrate" setting (safe for Dev/Staging only).
// In Production, prefer controlled migrations through CI/CD pipelines.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokeSyncDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();


    bool autoMigrateEnabled = config.GetValue<bool>("AutoMigrate");

    if (autoMigrateEnabled && (env.IsDevelopment() || env.IsEnvironment("Staging")))
    {
        await db.Database.MigrateAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(options =>
    {
        options.Title = "PokeSync API";
    });
}

app.MapControllers();
app.Run();
