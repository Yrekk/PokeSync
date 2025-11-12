using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Data;
using PokeSync.Api.Contracts.Idempotency;

namespace PokeSync.Api.Controllers;

[ApiController]
[Route("internal/idempotency/status")]
public sealed class IdempotencyController : ControllerBase
{
    private readonly PokeSyncDbContext _db;

    public IdempotencyController(PokeSyncDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Permet à MuleSoft ou à un outil d'observabilité interne de vérifier l'état d'une clé d'idempotence. Mulesoft pourra ainsi comparer l'item.count du payload initial à l'item count du payload lié à la clé et ainsi généré une nouvelle clé si il y a eu de la perte de données.
    /// </summary>
    /// <param name="key">Clé d'idempotence (X-Idempotency-Key)</param>
    /// <returns>Informations sur la clé si elle existe.</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(IdempotencyStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatusAsync(string key, CancellationToken ct)
    {
        var record = await _db.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalKey == key, ct);

        if (record is null)
            return NotFound(new IdempotencyStatusResponse { Exists = false });

        return Ok(new IdempotencyStatusResponse
        {
            Exists = true,
            ExternalKey = record.ExternalKey,
            CreatedUtc = record.CreatedUtc,
            PayloadHash = record.PayloadHash,
            ResponseBody = record.ResponseBody
        });
    }
}
