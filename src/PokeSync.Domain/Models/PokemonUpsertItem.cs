namespace PokeSync.Domain.Models
{
    public sealed class PokemonUpsertItem
    {
        public int ExternalId { get; init; }
        public int Number { get; init; }
        public string Name { get; init; } = "";
        public int GenerationNumber { get; init; }
        public IReadOnlyList<string> Types { get; init; } = Array.Empty<string>();
        public decimal? Height { get; init; }
        public decimal? Weight { get; init; }
        public string? SpriteUrl { get; init; }
        public IReadOnlyList<PokemonStatItem>? Stats { get; init; }
        public IReadOnlyList<PokemonFlavorItem>? Flavors { get; init; }
    }

    public sealed class PokemonStatItem
    {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    public sealed class PokemonFlavorItem
    {
        public int GenerationNumber { get; init; }
        public string Language { get; init; } = "";
        public string Text { get; init; } = "";
    }

}
