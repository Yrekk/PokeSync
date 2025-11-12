namespace PokeSync.Api.Contracts.Upsert
{
    public sealed record PokemonUpsertItemDto(
        int ExternalId,
        int Number,
        string Name,
        int GenerationNumber,
        string? SpriteUrl,
        decimal? Height,
        decimal? Weight,
        IReadOnlyList<string> Types,
        IReadOnlyList<PokemonStatDto>? Stats,
        IReadOnlyList<PokemonFlavorDto>? Flavors
    );

    public sealed record PokemonStatDto(string Name, int Value);

    public sealed record PokemonFlavorDto(
        int GenerationNumber,
        string Language,
        string Text
    );
}
