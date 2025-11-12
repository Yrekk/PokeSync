using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Data;
using PokeSync.Infrastructure.Data.Entities;

namespace PokeSync.Infrastructure.Services;

public interface IIdempotencyService
{
    Task<(bool Exists, bool SamePayload, IdempotencyKey? Record)>
        CheckAsync(string key, string payload, CancellationToken ct);

    Task SaveAsync(string key, string payload, string? responseBody, CancellationToken ct);
}

public sealed class IdempotencyService : IIdempotencyService
{
    private readonly PokeSyncDbContext _db;

    public IdempotencyService(PokeSyncDbContext db) => _db = db;

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public async Task<(bool Exists, bool SamePayload, IdempotencyKey? Record)>
        CheckAsync(string key, string payload, CancellationToken ct)
    {
        var hash = ComputeHash(payload);
        var existing = await _db.IdempotencyKeys.FirstOrDefaultAsync(x => x.ExternalKey == key, ct);

        if (existing is null)
            return (false, false, null);

        return (true, existing.PayloadHash == hash, existing);
    }

    public async Task SaveAsync(string key, string payload, string? responseBody, CancellationToken ct)
    {
        var record = new IdempotencyKey
        {
            ExternalKey = key,
            PayloadHash = ComputeHash(payload),
            CreatedUtc = DateTime.UtcNow,
            ResponseBody = responseBody
        };

        _db.IdempotencyKeys.Add(record);
        await _db.SaveChangesAsync(ct);
    }
}
