namespace PokeSync.Domain.Interfaces
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig> GetAsync(CancellationToken ct);
        Task SetBootstrapInProgressAsync(bool inProgress, CancellationToken ct);
        Task SetLastSyncUtcAsync(DateTimeOffset utc, CancellationToken ct);
    }
}
