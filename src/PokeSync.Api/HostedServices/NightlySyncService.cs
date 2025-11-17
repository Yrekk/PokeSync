using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokeSync.Infrastructure.Interfaces;

namespace PokeSync.Api.HostedServices
{
    /// <summary>
    /// Service hébergé qui exécute une synchronisation nocturne planifiée.
    /// Version squelette :
    /// - Planifie une exécution tous les jours à 03:00 UTC.
    /// - Utilise IStatusService pour éviter les chevauchements (lock logique).
    /// - Ne fait pour l'instant qu'un stub (pas d'appel MuleSoft).
    /// </summary>
    public sealed class NightlySyncService : BackgroundService
    {
        private readonly ILogger<NightlySyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public NightlySyncService(
            ILogger<NightlySyncService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Flag de config pour désactiver facilement le service
            var enabled = _configuration.GetValue<bool?>("NightlySync:Enabled") ?? true;
            if (!enabled)
            {
                _logger.LogInformation("NightlySyncService is disabled via configuration. Exiting.");
                return;
            }

            _logger.LogInformation("NightlySyncService starting (skeleton mode, no MuleSoft calls yet).");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1) Calculer le délai jusqu'à la prochaine exécution (03:00 UTC)
                    var delay = GetDelayUntilNextRunUtc(hourUtc: 3, minuteUtc: 0);
                    _logger.LogInformation(
                        "NightlySyncService will run next sync in {DelayMinutes} minutes (at {NextRunUtc} UTC).",
                        delay.TotalMinutes,
                        DateTimeOffset.UtcNow.Add(delay));

                    // 2) Attendre jusqu'à 03:00 UTC (ou annulation)
                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("NightlySyncService cancellation requested before sync start.");
                        break;
                    }

                    // 3) Créer un scope pour récupérer les services (DbContext, IStatusService, etc.)
                    using var scope = _scopeFactory.CreateScope();
                    var status = scope.ServiceProvider.GetRequiredService<IStatusService>();

                    // 4) Tenter de démarrer la nightly (lock logique via SystemConfig)
                    var canStart = await status.TryBeginNightlySyncAsync(stoppingToken);
                    if (!canStart)
                    {
                        _logger.LogWarning("Nightly sync not started: another instance or process is already handling it, or system is initializing.");
                        continue;
                    }

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        _logger.LogInformation("Nightly sync started (skeleton). No MuleSoft call implemented yet.");

                        // 5) SQUELETTE : ici on mettra plus tard l'appel MuleSoft /exp/pokemons/delta
                        // Pour l'instant, on simule juste un petit traitement.
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                        sw.Stop();
                        _logger.LogInformation("Nightly sync completed successfully in {ElapsedMs} ms (skeleton).", sw.ElapsedMilliseconds);

                        // 6) Marquer la sync comme réussie
                        await status.CompleteNightlySyncSuccessAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.LogError(ex, "Nightly sync failed after {ElapsedMs} ms (skeleton).", sw.ElapsedMilliseconds);

                        // 7) Marquer la sync comme échouée
                        await status.CompleteNightlySyncFailureAsync(ex.Message, stoppingToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Arrêt propre pendant le delay
                    _logger.LogInformation("NightlySyncService cancelled during delay. Exiting loop.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NightlySyncService encountered an unexpected error in the main loop. Will retry next day.");
                    // En cas d'erreur imprévue, on attend 24h avant de retenter
                    try
                    {
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogInformation("NightlySyncService cancelled during error backoff delay. Exiting.");
                        break;
                    }
                }
            }

            _logger.LogInformation("NightlySyncService stopping.");
        }

        /// <summary>
        /// Calcule le TimeSpan à attendre pour atteindre la prochaine occurrence d'une heure UTC donnée (ex: 03:00 UTC).
        /// </summary>
        private static TimeSpan GetDelayUntilNextRunUtc(int hourUtc, int minuteUtc)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var next = new DateTimeOffset(
                nowUtc.Year,
                nowUtc.Month,
                nowUtc.Day,
                hourUtc,
                minuteUtc,
                0,
                TimeSpan.Zero);

            if (next <= nowUtc)
            {
                next = next.AddDays(1);
            }

            return next - nowUtc;
        }
    }
}
