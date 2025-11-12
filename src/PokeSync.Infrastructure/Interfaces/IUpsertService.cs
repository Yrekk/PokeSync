using System.Linq.Expressions;

namespace PokeSync.Infrastructure.Interfaces
{
    public interface IUpsertService
    {
        Task<(int inserted, int skipped)> UpsertAsync<TEntity, TDto, TKey>(
            IEnumerable<TDto> dtos,
            Expression<Func<TEntity, TKey>> entityKeySelector,
            Func<TDto, TKey> dtoKeySelector,
            Func<TDto, TEntity> mapNew,
            IEqualityComparer<TKey>? keyComparer = null,
            CancellationToken ct = default)
            where TEntity : class;
    }

}
