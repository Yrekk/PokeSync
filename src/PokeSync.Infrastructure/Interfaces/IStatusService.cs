using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Infrastructure.Interfaces
{
    public sealed record StatusDto(bool Initializing, DateTimeOffset? LastSyncUtc);

    public interface IStatusService
    {
        Task<StatusDto> GetAsync(CancellationToken ct);
        Task MarkSyncNowAsync(CancellationToken ct);           // à appeler après Nightly/Upsert
        Task SetBootstrapAsync(bool inProgress, CancellationToken ct); // hooks bootstrap
    }
}
