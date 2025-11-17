
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using PokeSync.Domain.Entities;
using PokeSync.Infrastructure.Data;
using PokeSync.Infrastructure.Interfaces;
using System;

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
        var hasTypes = await _db.Set<Type>().AsNoTracking().AnyAsync(ct);
        var hasGenerations = await _db.Set<Generation>().AsNoTracking().AnyAsync(ct);
        var cfg = await _repo.GetAsync(ct);

        var initializing = (!hasTypes && !hasGenerations) || cfg.BootstrapInProgress;
        var dto = new StatusDto(initializing, cfg.LastSyncUtc);

        _cache.Set(CacheKey, dto, TimeSpan.FromSeconds(2)); // TTL 2s
        return dto;
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
}
