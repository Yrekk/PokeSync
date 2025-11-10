using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    public class PokemonFlavor
    {

        public int Id { get; set; }
        public int PokemonId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        //Navigation
        public Pokemon? Pokemon { get; set; }
    }
}
