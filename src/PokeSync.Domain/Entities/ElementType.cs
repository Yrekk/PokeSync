using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    public class ElementType
    {
        public int Id {  get; set; }
        public string Name { get; set; } = string.Empty;

        //Navigation

        public ICollection<PokemonType> PokemonTypes { get; set; } = new List<PokemonType>();
    }
}
