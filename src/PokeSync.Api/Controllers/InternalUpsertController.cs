using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeSync.Api.Contracts;
using PokeSync.Domain.Entities;
using PokeSync.Infrastructure.Data;
using PokeSync.Infrastructure.Services;

namespace PokeSync.Api.Controllers;

[ApiController]
[Route("internal/upsert")]
public sealed class InternalUpsertController : ControllerBase
{
    private readonly IUpsertService _upsert;
    private readonly ILogger<InternalUpsertController> _logger;
    private readonly PokeSyncDbContext _db;

    public InternalUpsertController(IUpsertService upsert, ILogger<InternalUpsertController> logger, PokeSyncDbContext db)
    {
        _upsert = upsert;
        _logger = logger;
        _db = db;
    }

    [HttpPost("types")]
    public async Task<IActionResult> UpsertTypes([FromBody] IEnumerable<TypeDto> payload, CancellationToken ct)
    {
        // Normalisation des noms (lower + trim) pour éviter les faux doublons
        var normalized = payload
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new TypeDto { Name = p.Name.Trim().ToLowerInvariant() })
            .ToList();

        var (inserted, skipped) = await _upsert.UpsertAsync<ElementType, TypeDto, string>(
            normalized,
            e => e.Name,
            d => d.Name,
            d => new ElementType { Name = d.Name },
            StringComparer.OrdinalIgnoreCase,
            ct);

        _logger.LogInformation("Upsert Types processed. Inserted={Inserted}, Skipped={Skipped}", inserted, skipped);
        return Ok(new { inserted, skipped });
    }

    [HttpPost("generations")]
    public async Task<IActionResult> UpsertGenerations([FromBody] IEnumerable<GenerationDto> payload, CancellationToken ct)
    {
        var valid = payload
            .Where(p => p.Number > 0 && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new GenerationDto { Number = p.Number, Name = p.Name.Trim().ToLowerInvariant() })
            .ToList();

        var (inserted, skipped) = await _upsert.UpsertAsync<Generation, GenerationDto, int>(
            valid,
            e => e.Number,
            d => d.Number,
            d => new Generation { Number = d.Number, Name = d.Name },
            EqualityComparer<int>.Default,
            ct);

        _logger.LogInformation("Upsert Generations processed. Inserted={Inserted}, Skipped={Skipped}");
        return Ok(new { inserted, skipped });
    }
}
