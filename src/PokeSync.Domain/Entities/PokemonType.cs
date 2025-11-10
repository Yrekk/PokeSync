using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Domain.Entities
{

    //Link between Pokemon Entity and Type Entity
    public class PokemonType
    {
        public int PokemonId { get; set; }
        public int TypeId { get; set; }

        //Navigation
        public Pokemon? Pokemon { get; set; }
        public ElementType? ElementType { get; set; }
    }

}
