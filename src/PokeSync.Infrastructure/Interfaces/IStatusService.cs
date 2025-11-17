// PokeSync.Infrastructure.Interfaces ou équivalent
public sealed record StatusDto(bool Initializing, DateTimeOffset? LastSyncUtc);

public interface IStatusService
{
    Task<StatusDto> GetAsync(CancellationToken ct);

    Task MarkSyncNowAsync(CancellationToken ct);

    // 🔹 Bootstrap (au démarrage)
    /// <summary>
    /// Tente de démarrer un bootstrap.
    /// Retourne false si un bootstrap est déjà en cours ou si le système est déjà prêt.
    /// </summary>
    Task<bool> TryBeginBootstrapAsync(CancellationToken ct);

    /// <summary>
    /// Marque le bootstrap comme réussi : state=ready, BootstrapInProgress=false, LastSyncUtc=UtcNow.
    /// </summary>
    Task CompleteBootstrapSuccessAsync(CancellationToken ct);

    /// <summary>
    /// Marque le bootstrap comme échoué : state=degraded, BootstrapInProgress=false, LastSyncError=message.
    /// </summary>
    Task CompleteBootstrapFailureAsync(string error, CancellationToken ct);

    // 🔹 Nightly sync
    /// <summary>
    /// Tente de démarrer une sync nocturne.
    /// Retourne false si une sync est déjà en cours ou si le système est en initializing.
    /// </summary>
    Task<bool> TryBeginNightlySyncAsync(CancellationToken ct);

    /// <summary>
    /// Marque la sync nocturne comme réussie : state=ready, SyncInProgress=false, LastSyncUtc=UtcNow.
    /// </summary>
    Task CompleteNightlySyncSuccessAsync(CancellationToken ct);

    /// <summary>
    /// Marque la sync nocturne comme échouée : state=degraded, SyncInProgress=false, LastSyncError=message.
    /// </summary>
    Task CompleteNightlySyncFailureAsync(string error, CancellationToken ct);
}
