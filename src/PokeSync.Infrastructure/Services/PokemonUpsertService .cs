using Microsoft.EntityFrameworkCore;
using PokeSync.Domain.Entities;
using PokeSync.Infrastructure.Data;
using PokeSync.Domain.Models;
using PokeSync.Domain.Interfaces;
using System.Diagnostics;
using System.Globalization;

namespace PokeSync.Infrastructure.Services
{
    public sealed class PokemonUpsertService : IPokemonUpsertService
    {
        private readonly PokeSyncDbContext _db;
        private readonly IStatusService _status;
        public PokemonUpsertService(PokeSyncDbContext db, IStatusService status)
        {
            _db = db;
            _status = status;
        }

        static string ExtIdStr(int extId) => extId.ToString(CultureInfo.InvariantCulture);
        static string Nz(string? s) => s ?? string.Empty;

        public async Task<UpsertBatchResult> UpsertPokemonsAsync(
            IReadOnlyList<PokemonUpsertItem> batch,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var genByNumber = await _db.Generation
                .AsNoTracking()
                .ToDictionaryAsync(g => g.Number, ct);

            var typesByName = await _db.Types
                .AsNoTracking()
                .ToDictionaryAsync(t => t.Name, ct);

            var results = new List<UpsertItemResult>(batch.Count);
            int inserted = 0, updated = 0, skipped = 0, failed = 0;

            foreach (var dto in batch)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    if (!genByNumber.TryGetValue(dto.GenerationNumber, out var generation))
                    {
                        failed++;
                        results.Add(new UpsertItemResult { ExternalId = dto.ExternalId, Status = "Failed", Message = $"Unknown generationNumber={dto.GenerationNumber}." });
                        await tx.CommitAsync(ct);
                        continue;
                    }

                    var missingType = dto.Types.FirstOrDefault(t => !typesByName.ContainsKey(t));
                    if (missingType is not null)
                    {
                        failed++;
                        results.Add(new UpsertItemResult { ExternalId = dto.ExternalId, Status = "Failed", Message = $"Unknown type '{missingType}'." });
                        await tx.CommitAsync(ct);
                        continue;
                    }

                    var extId = ExtIdStr(dto.ExternalId);

                    var pokemon = await _db.Pokemons
                        .Include(p => p.PokemonTypes)
                        .Include(p => p.Stats)
                        .Include(p => p.Flavors)
                        .FirstOrDefaultAsync(p => p.ExternalId == extId, ct);

                    if (pokemon is null)
                    {
                        pokemon = new Pokemon
                        {
                            ExternalId = extId,
                            Number = dto.Number,
                            Name = dto.Name,
                            GenerationId = generation.Id,
                            Height = dto.Height ?? 0m,
                            Weight = dto.Weight ?? 0m,
                            SpriteUrl = Nz(dto.SpriteUrl)
                        };
                        _db.Pokemons.Add(pokemon);

                        // Types
                        pokemon.PokemonTypes = dto.Types
                            .Select(t => new PokemonType { Pokemon = pokemon, TypeId = typesByName[t].Id })
                            .ToList();

                        // Stats
                        if (dto.Stats is not null)
                        {
                            pokemon.Stats = dto.Stats
                                .Select(s => new PokemonStat { StatName = s.Name, BaseValue = s.Value })
                                .ToList();
                        }

                        // Flavors
                        if (dto.Flavors is not null)
                        {
                            pokemon.Flavors = dto.Flavors.Select(f => new PokemonFlavor
                            {
                                Language = f.Language,
                                Text = f.Text
                            }).ToList();
                        }

                        await _db.SaveChangesAsync(ct);
                        await tx.CommitAsync(ct);

                        inserted++;
                        results.Add(new UpsertItemResult { ExternalId = dto.ExternalId, Status = "Inserted" });
                        continue;
                    }

                    bool mutated = false;

                    if (pokemon.Number != dto.Number)
                    { 
                        pokemon.Number = dto.Number; 
                        mutated = true;
                    }
                    if (!string.Equals(pokemon.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
                    { 
                        pokemon.Name = dto.Name; mutated = true;
                    }
                    if (pokemon.GenerationId != generation.Id) {
                        pokemon.GenerationId = generation.Id; 
                        mutated = true;
                    }
                    if (pokemon.Height != dto.Height)
                    {
                        pokemon.Height = dto.Height ?? 0m;
                        mutated = true;
                    }
                    if (pokemon.Weight != dto.Weight)
                    {
                        pokemon.Weight = dto.Weight ?? 0m;
                        mutated = true;
                    }
                    if (!string.Equals(pokemon.SpriteUrl, dto.SpriteUrl, StringComparison.Ordinal))
                    {
                        pokemon.SpriteUrl = dto.SpriteUrl;
                        mutated = true;
                    }

                    var desiredTypeIds = dto.Types.Select(t => typesByName[t].Id).OrderBy(x => x).ToArray();
                    var currentTypeIds = pokemon.PokemonTypes.Select(pt => pt.TypeId).OrderBy(x => x).ToArray();
                    
                    if (!desiredTypeIds.SequenceEqual(currentTypeIds))
                    {
                        _db.PokemonTypes.RemoveRange(pokemon.PokemonTypes);
                        pokemon.PokemonTypes = desiredTypeIds.Select(id => new PokemonType { PokemonId = pokemon.Id, TypeId = id }).ToList();
                        mutated = true;
                    }

                    if (dto.Stats is not null)
                    {
                        _db.PokemonStats.RemoveRange(pokemon.Stats);
                        pokemon.Stats = dto.Stats
                            .Select(s => new PokemonStat { StatName = s.Name, BaseValue = s.Value, PokemonId = pokemon.Id })
                            .ToList();
                        mutated = true;
                    }

                    if (dto.Flavors is not null)
                    {
                        _db.PokemonFlavors.RemoveRange(pokemon.Flavors);
                        pokemon.Flavors = dto.Flavors.Select(f => new PokemonFlavor
                        {
                            PokemonId = pokemon.Id,
                            Language = f.Language,
                            Text = f.Text
                        }).ToList();
                        mutated = true;
                    }

                    if (mutated)
                    {
                        await _db.SaveChangesAsync(ct);
                        await tx.CommitAsync(ct);

                        updated++;
                        results.Add(new UpsertItemResult { ExternalId = dto.ExternalId, Status = "Updated" });
                    }
                    else
                    {
                        await tx.CommitAsync(ct); ;
                        skipped++;
                        results.Add(new UpsertItemResult { ExternalId = dto.ExternalId, Status = "Skipped", Message = "No changes." });
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    try { await tx.RollbackAsync(ct); } catch { /* already completed, ignore */ }

                    results.Add(new UpsertItemResult
                    {
                        ExternalId = dto.ExternalId,
                        Status = "Failed",
                        Message = ex.Message
                    });
                }
            }

            sw.Stop();
            //HOOK readiness
            if ((inserted + updated) > 0)
            {
                await _status.MarkSyncNowAsync(ct);
            }
            return new UpsertBatchResult
            {
                Items = results,
                InsertedCount = inserted,
                UpdatedCount = updated,
                SkippedCount = skipped,
                FailedCount = failed,
                Duration = sw.Elapsed
            };
        }
    }
}
