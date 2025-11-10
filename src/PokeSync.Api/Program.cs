using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Data;



var builder = WebApplication.CreateBuilder(args);

//DbContext
builder.Services.AddDbContext<PokeSyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Controllers + minimal setup
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
