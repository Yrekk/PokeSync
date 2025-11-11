using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace PokeSync.Infrastructure.Services
{
    public interface IUpsertService
    {

        Task<(int inserted, int skipped)> UpsertAsync<TEntity, TDto, Tkey>(
                                                                         IEnumerable<TDto> dtos,
                                                                         Expression<Func<TEntity, Tkey>> entityKeySelector,
                                                                         Func<TDto, Tkey> dtoKeySelector,
                                                                         Func<TDto, TEntity> mapNew,
                                                                         IEqualityComparer<Tkey>? keyComparer = null,
                                                                         CancellationToken ct = default)
                                                                          where TEntity : class;
                                                                                   
    }

    public sealed class UpsertService : IUpsertService
    {
        private readonly PokeSyncDbContext _db;

        public UpsertService(PokeSyncDbContext db) => _db = db;

        public async Task<(int inserted, int skipped)> UpsertAsync<TEntity, TDto, TKey>(
            IEnumerable<TDto> dtos,
            Expression<Func<TEntity, TKey>> entityKeySelector,
            Func<TDto, TKey> dtoKeySelector,
            Func<TDto, TEntity> mapNew,
            IEqualityComparer<TKey>? keyComparer = null,
            CancellationToken ct = default)
            where TEntity : class
        {
            keyComparer ??= EqualityComparer<TKey>.Default;

            // 1) Distinct sur les clés entrantes
            var incoming = dtos.ToList();
            var incomingKeys = incoming
                .Select(dtoKeySelector)
                .Distinct(keyComparer)
                .ToList();

            if (!incomingKeys.Any())
                return (0, 0);

            // 2) Charger les clés existantes en base
            var existingKeys = await _db.Set<TEntity>()
                .Select(entityKeySelector)
                .ToListAsync(ct);

            var existingSet = new HashSet<TKey>(existingKeys, keyComparer);

            // 3) Sélectionner les nouveaux à insérer
            var toInsert = incoming
                .Where(d => !existingSet.Contains(dtoKeySelector(d)))
                .Select(mapNew)
                .ToList();

            if (toInsert.Count > 0)
            {
                await _db.Set<TEntity>().AddRangeAsync(toInsert, ct);
                await _db.SaveChangesAsync(ct);
            }

            var inserted = toInsert.Count;
            var skipped = incomingKeys.Count - inserted;
            return (inserted, skipped);
        }
    }
}
