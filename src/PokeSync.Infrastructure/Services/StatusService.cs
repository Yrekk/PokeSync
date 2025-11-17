
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PokeSync.Domain.Entities;
using PokeSync.Infrastructure.Data;
using PokeSync.Domain.Interfaces;
using ElementType = PokeSync.Domain.Entities.ElementType;

public sealed class StatusService : IStatusService
{
    private const string CacheKey = "status:get";
    private readonly IMemoryCache _cache;
    private readonly PokeSyncDbContext _db;
    private readonly ISystemConfigRepository _repo;

    public StatusService(IMemoryCache cache, PokeSyncDbContext db, ISystemConfigRepository repo)
    {
        _cache = cache; _db = db; _repo = repo;
    }

    public async Task<StatusDto> GetAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out StatusDto cached))
            return cached;

        // Règle métier: base vide si aucun Type ET aucune Generation
        var hasTypes = await _db.Set<ElementType>().AsNoTracking().AnyAsync(ct);
        var hasGenerations = await _db.Set<Generation>().AsNoTracking().AnyAsync(ct);
        var cfg = await _repo.GetAsync(ct);

        var initializing = (!hasTypes && !hasGenerations) || cfg.BootstrapInProgress;
        var dto = new StatusDto(initializing, cfg.LastSyncUtc);

        _cache.Set(CacheKey, dto, TimeSpan.FromSeconds(2)); // TTL 2s
        return dto;
    }

    private async Task<SystemConfig> LoadConfigForUpdateAsync(CancellationToken ct)
    {
        // On charge la ligne singleton Id=1 en mode "tracked"
        var cfg = await _db.SystemConfig
            .SingleAsync(x => x.Id == 1, ct);

        return cfg;
    }
    private void TouchUpdatedAt(SystemConfig cfg)
    {
        cfg.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    public async Task MarkSyncNowAsync(CancellationToken ct)
    {
        await _repo.SetLastSyncUtcAsync(DateTimeOffset.UtcNow, ct);
        _cache.Remove(CacheKey);
    }

    public async Task SetBootstrapAsync(bool inProgress, CancellationToken ct)
    {
        await _repo.SetBootstrapInProgressAsync(inProgress, ct);
        _cache.Remove(CacheKey);
    }

    public async Task<bool> TryBeginBootstrapAsync(CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        // Si déjà en bootstrap, ou déjà prêt (state=ready), on ne relance pas
        if (cfg.BootstrapInProgress || string.Equals(cfg.State, "ready", StringComparison.OrdinalIgnoreCase))
            return false;

        cfg.BootstrapInProgress = true;
        cfg.State = "initializing";
        cfg.SyncInProgress = false; // par sécurité
        cfg.LastSyncError = null;
        TouchUpdatedAt(cfg);

        try
        {
            await _db.SaveChangesAsync(ct);
            InvalidateCache();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // quelqu'un d'autre a pris le lock entre temps
            return false;
        }
    }
    public async Task CompleteBootstrapSuccessAsync(CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        cfg.BootstrapInProgress = false;
        cfg.State = "ready";
        cfg.LastSyncError = null;
        cfg.LastSyncUtc = DateTimeOffset.UtcNow;
        TouchUpdatedAt(cfg);

        await _db.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task CompleteBootstrapFailureAsync(string error, CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        cfg.BootstrapInProgress = false;
        cfg.State = "degraded";
        cfg.LastSyncError = error;
        TouchUpdatedAt(cfg);

        await _db.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task<bool> TryBeginNightlySyncAsync(CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        // Pas de nightly si système encore en initialization ou déjà en sync
        if (cfg.BootstrapInProgress ||
            string.Equals(cfg.State, "initializing", StringComparison.OrdinalIgnoreCase) ||
            cfg.SyncInProgress)
            return false;

        cfg.SyncInProgress = true;
        cfg.LastSyncError = null;
        TouchUpdatedAt(cfg);

        try
        {
            await _db.SaveChangesAsync(ct);
            InvalidateCache();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Une autre instance / thread a pris le lock
            return false;
        }
    }

    public async Task CompleteNightlySyncSuccessAsync(CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        cfg.SyncInProgress = false;
        cfg.State = "ready";
        cfg.LastSyncError = null;
        cfg.LastSyncUtc = DateTimeOffset.UtcNow;
        TouchUpdatedAt(cfg);

        await _db.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task CompleteNightlySyncFailureAsync(string error, CancellationToken ct)
    {
        var cfg = await LoadConfigForUpdateAsync(ct);

        cfg.SyncInProgress = false;
        cfg.State = "degraded";
        cfg.LastSyncError = error;
        TouchUpdatedAt(cfg);

        await _db.SaveChangesAsync(ct);
        InvalidateCache();
    }

}
