using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokeSync.Infrastructure.Data;
using PokeSync.Infrastructure.Interfaces;
using PokeSync.Domain.Entities;

namespace PokeSync.Api.HostedServices
{
    /// <summary>
    /// Service hébergé exécuté au démarrage.
    /// Son rôle est d'initialiser la base si les tables de référence sont vides.
    /// - En prod : appel MuleSoft (POST /exp/pokemons/init)
    /// - En dev/local : fallback seed-data.json
    /// L'état global est suivi dans SystemConfig via IStatusService.
    /// </summary>
    public sealed class BootstrapService : BackgroundService
    {
        private readonly ILogger<BootstrapService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public BootstrapService(
            ILogger<BootstrapService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _environment = environment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BootstrapService starting...");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PokeSyncDbContext>();
                var status = scope.ServiceProvider.GetRequiredService<IStatusService>();

                // 1) Si le système est arrêté, on sort proprement
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("BootstrapService cancelled before start.");
                    return;
                }

                // 2) Vérifier si les tables de référence sont déjà peuplées
                var hasTypes = await db.Types.AsNoTracking().AnyAsync(stoppingToken);
                var hasGenerations = await db.Generation.AsNoTracking().AnyAsync(stoppingToken);

                if (hasTypes && hasGenerations)
                {
                    _logger.LogInformation("Bootstrap skipped: Types and Generations already populated.");
                    return;
                }

                // 3) Tenter de démarrer le bootstrap (lock logique via SystemConfig)
                var canStart = await status.TryBeginBootstrapAsync(stoppingToken);
                if (!canStart)
                {
                    _logger.LogInformation("Bootstrap not started: another instance or thread is already handling it.");
                    return;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Bootstrap started. Environment: {EnvName}", _environment.EnvironmentName);

                    // 4) Choix du mode : MuleSoft (prod) ou fallback seed local (dev/local)
                    var mode = _configuration.GetValue<string>("Bootstrap:Mode");
                    if (string.IsNullOrWhiteSpace(mode))
                    {
                        mode = _environment.IsProduction() ? "MuleSoft" : "LocalSeed";
                    }

                    if (string.Equals(mode, "MuleSoft", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunMuleSoftBootstrapAsync(scope.ServiceProvider, stoppingToken);
                    }
                    else
                    {
                        await RunLocalSeedBootstrapAsync(scope.ServiceProvider, stoppingToken);
                    }

                    sw.Stop();
                    _logger.LogInformation("Bootstrap completed successfully in {Elapsed} ms.", sw.ElapsedMilliseconds);

                    // 5) Marquer le bootstrap comme réussi
                    await status.CompleteBootstrapSuccessAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Bootstrap failed after {Elapsed} ms.", sw.ElapsedMilliseconds);

                    // 6) Marquer le bootstrap comme échoué
                    await status.CompleteBootstrapFailureAsync(ex.Message, stoppingToken);
                }
            }
            catch (Exception outer)
            {
                // Cas vraiment extrême : erreur avant même de pouvoir charger les services.
                _logger.LogError(outer, "BootstrapService encountered a fatal error during startup.");
            }
            finally
            {
                _logger.LogInformation("BootstrapService finished.");
            }
        }

        /// <summary>
        /// Mode PROD (à brancher plus tard) : appel MuleSoft pour initialiser les référentiels.
        /// </summary>
        private static async Task RunMuleSoftBootstrapAsync(IServiceProvider services, CancellationToken ct)
        {
            // TODO: à implémenter dans l'EPIC MuleSoft.
            // Ici on prépare juste le squelette pour que ce soit plug & play.
            // Exemple futur:
            // var client = services.GetRequiredService<IMuleBootstrapClient>();
            // await client.TriggerInitialLoadAsync(ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Mode DEV/LOCAL : lit un fichier seed-data.json et peuple Types & Generations.
        /// </summary>
        private async Task RunLocalSeedBootstrapAsync(IServiceProvider services, CancellationToken ct)
        {
            var db = services.GetRequiredService<PokeSyncDbContext>();
            var env = services.GetRequiredService<IHostEnvironment>();

            // Chemin du fichier de seed (par ex. à la racine de l'API ou dans un dossier "Seed")
            var seedPath = Path.Combine(env.ContentRootPath, "Seed", "seed-data.json");

            if (!File.Exists(seedPath))
            {
                _logger.LogWarning("No seed-data.json found at {Path}. Local bootstrap will insert minimal sample data.", seedPath);

                // Fallback de secours : quelques données minimales pour le dev
                if (!await db.Types.AnyAsync(ct))
                {
                    db.Types.AddRange(
                        new ElementType { Name = "normal" },
                        new ElementType { Name = "fire" },
                        new ElementType { Name = "water" }
                    );
                }

                if (!await db.Generation.AnyAsync(ct))
                {
                    db.Generation.AddRange(
                        new Generation { Number = 1, Name = "Generation I" },
                        new Generation { Number = 2, Name = "Generation II" }
                    );
                }

                await db.SaveChangesAsync(ct);
                return;
            }

            _logger.LogInformation("Loading seed data from {Path}", seedPath);

            using var stream = File.OpenRead(seedPath);
            var seed = await JsonSerializer.DeserializeAsync<SeedData>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct);

            if (seed is null)
            {
                _logger.LogWarning("Seed file {Path} is empty or invalid. No data inserted.", seedPath);
                return;
            }

            // Types
            if (!await db.Types.AnyAsync(ct) && seed.Types?.Count > 0)
            {
                foreach (var t in seed.Types)
                {
                    db.Types.Add(new ElementType { Name = t.Name });
                }
            }

            // Generations
            if (!await db.Generation.AnyAsync(ct) && seed.Generations?.Count > 0)
            {
                foreach (var g in seed.Generations)
                {
                    db.Generation.Add(new Generation { Number = g.Number, Name = g.Name });
                }
            }

            await db.SaveChangesAsync(ct);
        }

        // DTOs pour la désérialisation du seed-data.json
        private sealed class SeedData
        {
            public List<SeedType> Types { get; set; } = new();
            public List<SeedGeneration> Generations { get; set; } = new();
        }

        private sealed class SeedType
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class SeedGeneration
        {
            public int Number { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
