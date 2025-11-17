using PokeSync.Infrastructure.Data;
using PokeSync.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PokeSync.Infrastructure.Services
{
    public sealed class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly PokeSyncDbContext _db;
        public SystemConfigRepository(PokeSyncDbContext db) => _db = db;

        public async Task<SystemConfig> GetAsync(CancellationToken ct) =>
            await _db.Set<SystemConfig>().AsNoTracking().SingleAsync(x => x.Id == 1, ct);

        public async Task SetBootstrapInProgressAsync(bool inProgress, CancellationToken ct)
        {
            var row = await _db.Set<SystemConfig>().SingleAsync(x => x.Id == 1, ct);
            row.BootstrapInProgress = inProgress;
            await _db.SaveChangesAsync(ct);
        }

        public async Task SetLastSyncUtcAsync(DateTimeOffset utc, CancellationToken ct)
        {
            var row = await _db.Set<SystemConfig>().SingleAsync(x => x.Id == 1, ct);
            row.LastSyncUtc = utc;
            await _db.SaveChangesAsync(ct);
        }
    }
}
