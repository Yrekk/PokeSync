using PokeSync.Domain.Models;
namespace PokeSync.Domain.Interfaces
{
    public interface IPokemonUpsertService
    {
        Task<UpsertBatchResult> UpsertPokemonsAsync(
            IReadOnlyList<PokemonUpsertItem> batch,
            CancellationToken ct);
    }
}
