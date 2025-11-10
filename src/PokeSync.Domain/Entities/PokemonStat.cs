using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{
    public class PokemonStat
    {
        public int Id { get; set; }
        public int PokemonId { get; set; }
        public string StatName { get; set; } = string.Empty;
        public int BaseValue { get; set; }

        //Navigation
        public Pokemon? Pokemon { get; set; }
    }
}
