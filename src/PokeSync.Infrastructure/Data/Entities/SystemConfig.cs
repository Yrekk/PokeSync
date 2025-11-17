namespace PokeSync.Infrastructure.Data.Entities;

public sealed class SystemConfig
{
    public int Id { get; set; } = 1;
    public string State { get; set; } = "initializing"; // "initializing" | "ready" | "degraded"
    public bool SyncInProgress { get; set; }
    public bool BootstrapInProgress { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
    public string? LastSyncError { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // concurrency
}
