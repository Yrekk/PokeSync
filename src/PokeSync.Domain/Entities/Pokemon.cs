using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    public class Pokemon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public  int Number {  get; set; }
        public int GenerationId {  get; set; }
        public string SpriteUrl { get; set; } = string.Empty;
        public decimal Height { get; set; }
        public decimal Weight { get; set; }

        //Navigation
        public Generation? Generation {  get; set; }
        public ICollection<PokemonType> PokemonTypes { get; set; } = new List<PokemonType>();
        public ICollection<PokemonStat> Stats { get; set; } = new List<PokemonStat>();
        public ICollection<PokemonFlavor> Flavors { get; set; } = new List<PokemonFlavor>();

    }
}
