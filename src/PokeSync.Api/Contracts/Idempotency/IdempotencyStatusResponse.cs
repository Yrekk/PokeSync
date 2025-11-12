namespace PokeSync.Api.Contracts.Idempotency;

public sealed class IdempotencyStatusResponse
{
    public bool Exists { get; init; }
    public DateTime? CreatedUtc { get; init; }
    public string? ExternalKey { get; init; }
    public string? PayloadHash { get; init; }
    public string? ResponseBody { get; init; } // Optionnel : utile si Mule veut la réponse
}
