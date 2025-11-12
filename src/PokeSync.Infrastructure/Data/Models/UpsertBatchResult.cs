namespace PokeSync.Infrastructure.Data.Models;

public sealed class UpsertBatchResult
{
    public required IReadOnlyList<UpsertItemResult> Items { get; init; }
    public int InsertedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int SkippedCount { get; init; }
    public int FailedCount { get; init; }
    public TimeSpan Duration { get; init; }
}

public sealed class UpsertItemResult
{
    public required int ExternalId { get; init; }
    public required string Status { get; init; } // "Inserted"|"Updated"|"Skipped"|"Failed"
    public string? Message { get; init; }
}
