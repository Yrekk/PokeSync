using System.ComponentModel.DataAnnotations;

namespace PokeSync.Infrastructure.Data.Entities;

public sealed class IdempotencyKey
{
    [Key]
    public int Id { get; set; } // PK identity

    [Required]
    [MaxLength(128)]
    public string ExternalKey { get; set; } = null!; // X-Idempotency-Key (unique)

    [Required]
    [MaxLength(64)]
    public string PayloadHash { get; set; } = null!; // SHA-256 hex (64)

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Optionnel : conserver la réponse JSON renvoyée à MuleSoft
    public string? ResponseBody { get; set; } // nvarchar(max)
}
