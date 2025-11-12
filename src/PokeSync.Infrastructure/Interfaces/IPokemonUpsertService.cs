using PokeSync.Infrastructure.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Infrastructure.Interfaces
{
    public interface IPokemonUpsertService
    {
        Task<UpsertBatchResult> UpsertPokemonsAsync(
            IReadOnlyList<PokemonUpsertItem> batch,
            CancellationToken ct);
    }
}
