using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Data;
using PokeSync.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace PokeSync.Infrastructure.Services
{
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

            // 1) Prépare les clés entrantes (distinct côté mémoire)
            var incoming = dtos.ToList();
            var incomingKeys = incoming.Select(dtoKeySelector).Distinct(keyComparer).ToList();
            if (incomingKeys.Count == 0)
                return (0, 0);

            // 2) Charge SEULEMENT les clés existantes qui nous intéressent
            //    => Select(key) .Where(incoming.Contains(key)) pour laisser le filtrage au SQL
            var existingKeys = await _db.Set<TEntity>()
                .AsNoTracking()
                .Select(entityKeySelector)
                .Where(k => incomingKeys.Contains(k)) // filtrage côté DB
                .ToListAsync(ct);

            var existingSet = new HashSet<TKey>(existingKeys, keyComparer);

            // 3) Mappe uniquement les nouveaux
            var toInsert = incoming
                .Where(d => !existingSet.Contains(dtoKeySelector(d)))
                .Select(mapNew)
                .ToList();

            if (toInsert.Count == 0)
                return (0, incomingKeys.Count);

            try
            {
                await _db.Set<TEntity>().AddRangeAsync(toInsert, ct);
                await _db.SaveChangesAsync(ct);
                var inserted = toInsert.Count;
                var skipped = incomingKeys.Count - inserted;
                return (inserted, skipped);
            }
            catch (DbUpdateException)
            {
                // En cas de course (unique constraint), on peut recomptabiliser comme "skip"
                // ou relire pour savoir exactement. Ici, on considère que tout ce qui a échoué
                // sur contrainte unique est "skip".
                return (0, incomingKeys.Count);
            }
        }
    }
}
